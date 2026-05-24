using HRMS.Application.DTOs;
using HRMS.Domain.Entities;
using HRMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HRMS.API.Services;

public class AccessControlService
{
    private const int MenuSortInterval = 20;
    private readonly HrmsDbContext _dbContext;
    private readonly TokenSessionStore _tokenSessionStore;

    public AccessControlService(HrmsDbContext dbContext, TokenSessionStore tokenSessionStore)
    {
        _dbContext = dbContext;
        _tokenSessionStore = tokenSessionStore;
    }

    public async Task<LoginResponseDto?> LoginAsync(string username, string password)
    {
        try
        {
            var normalized = username.Trim();
            var user = await _dbContext.SysUsers
                .Include(x => x.Post)
                .Include(x => x.UserRoles)
                    .ThenInclude(x => x.Role)
                .FirstOrDefaultAsync(x =>
                    x.IsActive &&
                    (x.Username == normalized || x.Phone == normalized || (x.Employee != null && x.Employee.EmployeeNo == normalized)));

            if (user == null || user.Password != password)
            {
                return null;
            }

            var permissions = await GetUserPermissionCodesAsync(user.Id);
            var token = $"rbac-token-{Guid.NewGuid():N}";
            await _tokenSessionStore.SetAsync(token, new UserSessionContext(user.Id, user.IsAdmin, permissions.ToHashSet(StringComparer.OrdinalIgnoreCase)));

            var menus = await GetUserMenuTreeAsync(user.Id);

            return new LoginResponseDto
            {
                Token = token,
                UserInfo = MapUserProfile(user),
                Permissions = permissions.OrderBy(x => x).ToList(),
                Menus = menus
            };
        }
        catch
        {
            throw;
        }
    }

    public async Task LogoutAsync(string token)
    {
        await _tokenSessionStore.RemoveAsync(token);
    }

    public async Task<LoginResponseDto?> GetCurrentContextAsync(Guid userId, string token)
    {
        var user = await _dbContext.SysUsers
            .Include(x => x.Post)
            .Include(x => x.UserRoles)
                .ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x => x.Id == userId && x.IsActive);

        if (user == null)
        {
            return null;
        }

        var permissions = await GetUserPermissionCodesAsync(userId);
        await _tokenSessionStore.SetAsync(token, new UserSessionContext(user.Id, user.IsAdmin, permissions.ToHashSet(StringComparer.OrdinalIgnoreCase)));

        return new LoginResponseDto
        {
            Token = token,
            UserInfo = MapUserProfile(user),
            Permissions = permissions.OrderBy(x => x).ToList(),
            Menus = await GetUserMenuTreeAsync(user.Id)
        };
    }

    public async Task<List<RoleDto>> GetRolesAsync()
    {
        var roles = await _dbContext.SysRoles
            .Include(x => x.Permissions)
            .Include(x => x.UserRoles)
            .OrderBy(x => x.RoleCode)
            .ToListAsync();

        return roles.Select(x => new RoleDto
        {
            Id = x.Id,
            RoleCode = x.RoleCode,
            RoleName = x.RoleName,
            Description = x.Description,
            IsActive = x.IsActive,
            UserCount = x.UserRoles.Count,
            PermissionMenuIds = x.Permissions.Select(p => p.MenuId).Distinct().ToList()
        }).ToList();
    }

    public async Task<RoleDto?> GetRoleAsync(Guid id)
    {
        var role = await _dbContext.SysRoles
            .Include(x => x.Permissions)
            .Include(x => x.UserRoles)
            .FirstOrDefaultAsync(x => x.Id == id);

        return role == null
            ? null
            : new RoleDto
            {
                Id = role.Id,
                RoleCode = role.RoleCode,
                RoleName = role.RoleName,
                Description = role.Description,
                IsActive = role.IsActive,
                UserCount = role.UserRoles.Count,
                PermissionMenuIds = role.Permissions.Select(p => p.MenuId).Distinct().ToList()
            };
    }

    public async Task<RoleDto> CreateRoleAsync(CreateRoleDto dto)
    {
        if (await _dbContext.SysRoles.AnyAsync(x => x.RoleCode == dto.RoleCode))
        {
            throw new InvalidOperationException("角色编码已存在");
        }

        var role = new SysRole
        {
            RoleCode = dto.RoleCode.Trim(),
            RoleName = dto.RoleName.Trim(),
            Description = dto.Description?.Trim(),
            IsActive = dto.IsActive
        };

        _dbContext.SysRoles.Add(role);
        await _dbContext.SaveChangesAsync();
        return (await GetRoleAsync(role.Id))!;
    }

    public async Task<RoleDto?> UpdateRoleAsync(UpdateRoleDto dto)
    {
        var role = await _dbContext.SysRoles.FirstOrDefaultAsync(x => x.Id == dto.Id);
        if (role == null)
        {
            return null;
        }

        if (await _dbContext.SysRoles.AnyAsync(x => x.Id != dto.Id && x.RoleCode == dto.RoleCode))
        {
            throw new InvalidOperationException("角色编码已存在");
        }

        role.RoleCode = dto.RoleCode.Trim();
        role.RoleName = dto.RoleName.Trim();
        role.Description = dto.Description?.Trim();
        role.IsActive = dto.IsActive;
        role.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();
        return await GetRoleAsync(role.Id);
    }

    public async Task<bool> DeleteRoleAsync(Guid id)
    {
        var role = await _dbContext.SysRoles.FirstOrDefaultAsync(x => x.Id == id);
        if (role == null)
        {
            return false;
        }

        _dbContext.SysRolePermissions.RemoveRange(_dbContext.SysRolePermissions.Where(x => x.RoleId == id));
        _dbContext.SysUserRoles.RemoveRange(_dbContext.SysUserRoles.Where(x => x.RoleId == id));
        _dbContext.SysRoles.Remove(role);
        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task AssignRolePermissionsAsync(Guid roleId, List<Guid> menuIds)
    {
        var role = await _dbContext.SysRoles.FirstOrDefaultAsync(x => x.Id == roleId);
        if (role == null)
        {
            throw new InvalidOperationException("角色不存在");
        }

        var menuMap = await _dbContext.SysMenus
            .Where(x => menuIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x);

        _dbContext.SysRolePermissions.RemoveRange(_dbContext.SysRolePermissions.Where(x => x.RoleId == roleId));

        var permissions = menuMap.Values.Select(menu => new SysRolePermission
        {
            RoleId = roleId,
            MenuId = menu.Id,
            PermissionCode = menu.PermissionCode ?? menu.MenuKey,
            PermissionType = menu.MenuType
        });

        _dbContext.SysRolePermissions.AddRange(permissions);
        await _dbContext.SaveChangesAsync();
    }

    public async Task CopyRolePermissionsAsync(Guid roleId, Guid sourceRoleId)
    {
        if (roleId == sourceRoleId)
        {
            throw new InvalidOperationException("不能从当前角色复制自身权限");
        }

        var role = await _dbContext.SysRoles.FirstOrDefaultAsync(x => x.Id == roleId);
        if (role == null)
        {
            throw new InvalidOperationException("目标角色不存在");
        }

        var sourceRole = await _dbContext.SysRoles.FirstOrDefaultAsync(x => x.Id == sourceRoleId);
        if (sourceRole == null)
        {
            throw new InvalidOperationException("来源角色不存在");
        }

        var sourceMenuIds = await _dbContext.SysRolePermissions
            .Where(x => x.RoleId == sourceRoleId)
            .Select(x => x.MenuId)
            .Distinct()
            .ToListAsync();

        await AssignRolePermissionsAsync(roleId, sourceMenuIds);
    }

    public async Task<List<PostDto>> GetPostsAsync()
    {
        var posts = await _dbContext.SysPosts
            .Include(x => x.Permissions)
            .Include(x => x.Users)
            .OrderBy(x => x.PostCode)
            .ToListAsync();

        return posts.Select(x => new PostDto
        {
            Id = x.Id,
            PostCode = x.PostCode,
            PostName = x.PostName,
            Description = x.Description,
            IsActive = x.IsActive,
            UserCount = x.Users.Count,
            PermissionMenuIds = x.Permissions.Select(p => p.MenuId).Distinct().ToList()
        }).ToList();
    }

    public async Task<PostDto?> GetPostAsync(Guid id)
    {
        var post = await _dbContext.SysPosts
            .Include(x => x.Permissions)
            .Include(x => x.Users)
            .FirstOrDefaultAsync(x => x.Id == id);

        return post == null
            ? null
            : new PostDto
            {
                Id = post.Id,
                PostCode = post.PostCode,
                PostName = post.PostName,
                Description = post.Description,
                IsActive = post.IsActive,
                UserCount = post.Users.Count,
                PermissionMenuIds = post.Permissions.Select(p => p.MenuId).Distinct().ToList()
            };
    }

    public async Task<PostDto> CreatePostAsync(CreatePostDto dto)
    {
        if (await _dbContext.SysPosts.AnyAsync(x => x.PostCode == dto.PostCode))
        {
            throw new InvalidOperationException("岗位编码已存在");
        }

        var post = new SysPost
        {
            PostCode = dto.PostCode.Trim(),
            PostName = dto.PostName.Trim(),
            Description = dto.Description?.Trim(),
            IsActive = dto.IsActive
        };

        _dbContext.SysPosts.Add(post);
        await _dbContext.SaveChangesAsync();
        return (await GetPostAsync(post.Id))!;
    }

    public async Task<PostDto?> UpdatePostAsync(UpdatePostDto dto)
    {
        var post = await _dbContext.SysPosts.FirstOrDefaultAsync(x => x.Id == dto.Id);
        if (post == null)
        {
            return null;
        }

        if (await _dbContext.SysPosts.AnyAsync(x => x.Id != dto.Id && x.PostCode == dto.PostCode))
        {
            throw new InvalidOperationException("岗位编码已存在");
        }

        post.PostCode = dto.PostCode.Trim();
        post.PostName = dto.PostName.Trim();
        post.Description = dto.Description?.Trim();
        post.IsActive = dto.IsActive;
        post.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();
        return await GetPostAsync(post.Id);
    }

    public async Task<bool> DeletePostAsync(Guid id)
    {
        var post = await _dbContext.SysPosts.FirstOrDefaultAsync(x => x.Id == id);
        if (post == null)
        {
            return false;
        }

        var users = await _dbContext.SysUsers.Where(x => x.PostId == id).ToListAsync();
        foreach (var user in users)
        {
            user.PostId = null;
        }

        _dbContext.SysPostPermissions.RemoveRange(_dbContext.SysPostPermissions.Where(x => x.PostId == id));
        _dbContext.SysPosts.Remove(post);
        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task AssignPostPermissionsAsync(Guid postId, List<Guid> menuIds)
    {
        var post = await _dbContext.SysPosts.FirstOrDefaultAsync(x => x.Id == postId);
        if (post == null)
        {
            throw new InvalidOperationException("岗位不存在");
        }

        var menuMap = await _dbContext.SysMenus
            .Where(x => menuIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x);

        _dbContext.SysPostPermissions.RemoveRange(_dbContext.SysPostPermissions.Where(x => x.PostId == postId));

        var permissions = menuMap.Values.Select(menu => new SysPostPermission
        {
            PostId = postId,
            MenuId = menu.Id,
            PermissionCode = menu.PermissionCode ?? menu.MenuKey,
            PermissionType = menu.MenuType
        });

        _dbContext.SysPostPermissions.AddRange(permissions);
        await _dbContext.SaveChangesAsync();
    }

    public async Task CopyPostPermissionsAsync(Guid postId, Guid sourcePostId)
    {
        if (postId == sourcePostId)
        {
            throw new InvalidOperationException("不能从当前岗位复制自身权限");
        }

        var post = await _dbContext.SysPosts.FirstOrDefaultAsync(x => x.Id == postId);
        if (post == null)
        {
            throw new InvalidOperationException("目标岗位不存在");
        }

        var sourcePost = await _dbContext.SysPosts.FirstOrDefaultAsync(x => x.Id == sourcePostId);
        if (sourcePost == null)
        {
            throw new InvalidOperationException("来源岗位不存在");
        }

        var sourceMenuIds = await _dbContext.SysPostPermissions
            .Where(x => x.PostId == sourcePostId)
            .Select(x => x.MenuId)
            .Distinct()
            .ToListAsync();

        await AssignPostPermissionsAsync(postId, sourceMenuIds);
    }

    public async Task<List<MenuTreeNodeDto>> GetMenuTreeAsync(bool includeButtons = true, bool includeInactive = false)
    {
        var menus = await _dbContext.SysMenus
            .Where(x => (includeInactive || x.IsActive) && (includeButtons || x.MenuType != "button"))
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.CreatedAt)
            .ToListAsync();

        return BuildMenuTree(menus);
    }

    public async Task<MenuDto> CreateMenuAsync(CreateMenuDto dto)
    {
        if (await _dbContext.SysMenus.AnyAsync(x => x.MenuKey == dto.MenuKey))
        {
            throw new InvalidOperationException("菜单Key已存在");
        }

        if (!string.IsNullOrWhiteSpace(dto.PermissionCode) && await _dbContext.SysMenus.AnyAsync(x => x.PermissionCode == dto.PermissionCode))
        {
            throw new InvalidOperationException("权限编码已存在");
        }

        var menu = new SysMenu
        {
            ParentId = dto.ParentId,
            MenuKey = dto.MenuKey.Trim(),
            MenuName = dto.MenuName.Trim(),
            MenuType = dto.MenuType.Trim(),
            SortOrder = NormalizeMenuSortOrder(dto.SortOrder),
            RoutePath = dto.RoutePath?.Trim(),
            ComponentPath = dto.ComponentPath?.Trim(),
            Icon = dto.Icon?.Trim(),
            IsVisible = dto.IsVisible,
            IsActive = dto.IsActive,
            PermissionCode = dto.PermissionCode?.Trim()
        };

        _dbContext.SysMenus.Add(menu);
        await _dbContext.SaveChangesAsync();
        return MapMenu(menu);
    }

    public async Task<MenuDto?> UpdateMenuAsync(UpdateMenuDto dto)
    {
        var menu = await _dbContext.SysMenus.FirstOrDefaultAsync(x => x.Id == dto.Id);
        if (menu == null)
        {
            return null;
        }

        if (await _dbContext.SysMenus.AnyAsync(x => x.Id != dto.Id && x.MenuKey == dto.MenuKey))
        {
            throw new InvalidOperationException("菜单Key已存在");
        }

        if (!string.IsNullOrWhiteSpace(dto.PermissionCode) && await _dbContext.SysMenus.AnyAsync(x => x.Id != dto.Id && x.PermissionCode == dto.PermissionCode))
        {
            throw new InvalidOperationException("权限编码已存在");
        }

        menu.ParentId = dto.ParentId;
        menu.MenuKey = dto.MenuKey.Trim();
        menu.MenuName = dto.MenuName.Trim();
        menu.MenuType = dto.MenuType.Trim();
        menu.SortOrder = NormalizeMenuSortOrder(dto.SortOrder);
        menu.RoutePath = dto.RoutePath?.Trim();
        menu.ComponentPath = dto.ComponentPath?.Trim();
        menu.Icon = dto.Icon?.Trim();
        menu.IsVisible = dto.IsVisible;
        menu.IsActive = dto.IsActive;
        menu.PermissionCode = dto.PermissionCode?.Trim();
        menu.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();
        return MapMenu(menu);
    }

    public async Task<bool> DeleteMenuAsync(Guid id)
    {
        var menus = await _dbContext.SysMenus.OrderBy(x => x.SortOrder).ToListAsync();
        var targetIds = CollectDescendantIds(menus, id);
        if (targetIds.Count == 0)
        {
            return false;
        }

        _dbContext.SysRolePermissions.RemoveRange(_dbContext.SysRolePermissions.Where(x => targetIds.Contains(x.MenuId)));
        _dbContext.SysPostPermissions.RemoveRange(_dbContext.SysPostPermissions.Where(x => targetIds.Contains(x.MenuId)));
        _dbContext.SysMenus.RemoveRange(menus.Where(x => targetIds.Contains(x.Id)));
        await _dbContext.SaveChangesAsync();
        return true;
    }

    private static int NormalizeMenuSortOrder(int sortOrder)
    {
        if (sortOrder <= 0)
        {
            return MenuSortInterval;
        }

        var remainder = sortOrder % MenuSortInterval;
        if (remainder == 0)
        {
            return sortOrder;
        }

        return sortOrder + (MenuSortInterval - remainder);
    }

    public async Task<List<UserDto>> GetUsersAsync()
    {
        var users = await _dbContext.SysUsers
            .Include(x => x.Post)
            .Include(x => x.UserRoles)
                .ThenInclude(x => x.Role)
            .OrderBy(x => x.Username)
            .ToListAsync();

        return users.Select(MapUser).ToList();
    }

    public async Task<UserDto?> GetUserAsync(Guid id)
    {
        var user = await _dbContext.SysUsers
            .Include(x => x.Post)
            .Include(x => x.UserRoles)
                .ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x => x.Id == id);

        return user == null ? null : MapUser(user);
    }

    public async Task<UserPermissionContextDto?> GetUserPermissionContextAsync(Guid userId)
    {
        var user = await _dbContext.SysUsers
            .Include(x => x.Post)
            .Include(x => x.UserRoles)
                .ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x => x.Id == userId);
        if (user == null)
        {
            return null;
        }

        var rolePermissionMenuIds = await GetRolePermissionMenuIdsAsync(user.Id);
        var postPermissionMenuIds = await GetPostPermissionMenuIdsAsync(user.PostId);
        var grantedMenuIds = user.IsAdmin
            ? await _dbContext.SysMenus.Where(x => x.IsActive).Select(x => x.Id).ToListAsync()
            : rolePermissionMenuIds.Concat(postPermissionMenuIds).Distinct().ToList();
        var permissionCodes = user.IsAdmin
            ? await _dbContext.SysMenus
                .Where(x => x.IsActive && !string.IsNullOrWhiteSpace(x.PermissionCode))
                .Select(x => x.PermissionCode!)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync()
            : await GetPermissionCodesByMenuIdsAsync(grantedMenuIds);

        var allMenus = await _dbContext.SysMenus
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.CreatedAt)
            .ToListAsync();
        var visibleIds = ExpandWithAncestors(allMenus, grantedMenuIds);

        return new UserPermissionContextDto
        {
            UserId = user.Id,
            Username = user.Username,
            Name = user.Name,
            PostId = user.PostId,
            PostName = user.Post?.PostName,
            IsAdmin = user.IsAdmin,
            RoleIds = user.UserRoles.Select(x => x.RoleId).ToList(),
            RoleNames = user.UserRoles.Where(x => x.Role != null).Select(x => x.Role!.RoleName).ToList(),
            PermissionCodes = permissionCodes,
            GrantedMenuIds = grantedMenuIds,
            RolePermissionMenuIds = rolePermissionMenuIds,
            PostPermissionMenuIds = postPermissionMenuIds,
            MenuTree = BuildMenuTree(allMenus.Where(x => visibleIds.Contains(x.Id)).ToList())
        };
    }

    public async Task<UserDto> CreateUserAsync(CreateUserDto dto)
    {
        if (await _dbContext.SysUsers.AnyAsync(x => x.Username == dto.Username))
        {
            throw new InvalidOperationException("用户名已存在");
        }

        var user = new SysUser
        {
            Username = dto.Username.Trim(),
            Password = dto.Password,
            Name = dto.Name.Trim(),
            Phone = dto.Phone?.Trim(),
            Email = dto.Email?.Trim(),
            EmployeeId = dto.EmployeeId,
            PostId = dto.PostId,
            IsActive = dto.IsActive,
            IsAdmin = dto.IsAdmin
        };

        _dbContext.SysUsers.Add(user);
        await _dbContext.SaveChangesAsync();
        await SyncUserRolesAsync(user.Id, dto.RoleIds);
        return (await GetUserAsync(user.Id))!;
    }

    public async Task<UserDto?> UpdateUserAsync(UpdateUserDto dto)
    {
        var user = await _dbContext.SysUsers.FirstOrDefaultAsync(x => x.Id == dto.Id);
        if (user == null)
        {
            return null;
        }

        if (await _dbContext.SysUsers.AnyAsync(x => x.Id != dto.Id && x.Username == dto.Username))
        {
            throw new InvalidOperationException("用户名已存在");
        }

        user.Username = dto.Username.Trim();
        user.Name = dto.Name.Trim();
        user.Phone = dto.Phone?.Trim();
        user.Email = dto.Email?.Trim();
        user.EmployeeId = dto.EmployeeId;
        user.PostId = dto.PostId;
        user.IsActive = dto.IsActive;
        user.IsAdmin = dto.IsAdmin;
        user.UpdatedAt = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            user.Password = dto.Password;
        }

        await _dbContext.SaveChangesAsync();
        await SyncUserRolesAsync(user.Id, dto.RoleIds);
        return await GetUserAsync(user.Id);
    }

    public async Task<bool> DeleteUserAsync(Guid id)
    {
        var user = await _dbContext.SysUsers.FirstOrDefaultAsync(x => x.Id == id);
        if (user == null)
        {
            return false;
        }

        _dbContext.SysUserRoles.RemoveRange(_dbContext.SysUserRoles.Where(x => x.UserId == id));
        _dbContext.SysUsers.Remove(user);
        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<(List<RoleDto> Roles, List<PostDto> Posts)> GetAccessOptionsAsync()
    {
        return (await GetRolesAsync(), await GetPostsAsync());
    }

    public async Task<List<string>> GetUserPermissionCodesAsync(Guid userId)
    {
        var user = await _dbContext.SysUsers.FirstOrDefaultAsync(x => x.Id == userId && x.IsActive);
        if (user == null)
        {
            return new List<string>();
        }

        if (user.IsAdmin)
        {
            return await _dbContext.SysMenus
                .Where(x => x.IsActive && !string.IsNullOrWhiteSpace(x.PermissionCode))
                .Select(x => x.PermissionCode!)
                .Distinct()
                .ToListAsync();
        }

        var grantedMenuIds = (await GetRolePermissionMenuIdsAsync(userId))
            .Concat(await GetPostPermissionMenuIdsAsync(user.PostId))
            .Distinct()
            .ToList();

        return await GetPermissionCodesByMenuIdsAsync(grantedMenuIds);
    }

    public async Task<List<MenuTreeNodeDto>> GetUserMenuTreeAsync(Guid userId)
    {
        var allMenus = await _dbContext.SysMenus
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.CreatedAt)
            .ToListAsync();

        var user = await _dbContext.SysUsers.FirstOrDefaultAsync(x => x.Id == userId && x.IsActive);
        if (user == null)
        {
            return new List<MenuTreeNodeDto>();
        }

        if (user.IsAdmin)
        {
            return BuildMenuTree(allMenus.Where(x => x.IsVisible && x.MenuType != "button").ToList());
        }

        var permissionMenuIds = await GetUserPermissionMenuIdsAsync(userId);
        var visibleIds = ExpandWithAncestors(allMenus, permissionMenuIds);
        var visibleMenus = allMenus
            .Where(x => visibleIds.Contains(x.Id) && x.IsVisible && x.MenuType != "button")
            .ToList();

        return BuildMenuTree(visibleMenus);
    }

    private async Task<List<Guid>> GetUserPermissionMenuIdsAsync(Guid userId)
    {
        var user = await _dbContext.SysUsers.FirstOrDefaultAsync(x => x.Id == userId && x.IsActive);
        if (user == null)
        {
            return new List<Guid>();
        }

        if (user.IsAdmin)
        {
            return await _dbContext.SysMenus.Where(x => x.IsActive).Select(x => x.Id).ToListAsync();
        }

        var roleMenuIds = await GetRolePermissionMenuIdsAsync(userId);
        var postMenuIds = await GetPostPermissionMenuIdsAsync(user.PostId);

        return roleMenuIds.Concat(postMenuIds).Distinct().ToList();
    }

    private async Task SyncUserRolesAsync(Guid userId, List<Guid> roleIds)
    {
        _dbContext.SysUserRoles.RemoveRange(_dbContext.SysUserRoles.Where(x => x.UserId == userId));

        var validRoleIds = await _dbContext.SysRoles
            .Where(x => roleIds.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync();

        var rows = validRoleIds.Select(roleId => new SysUserRole
        {
            UserId = userId,
            RoleId = roleId
        });

        _dbContext.SysUserRoles.AddRange(rows);
        await _dbContext.SaveChangesAsync();
    }

    private async Task<List<Guid>> GetRolePermissionMenuIdsAsync(Guid userId)
    {
        return await _dbContext.SysUserRoles
            .Where(x => x.UserId == userId && x.Role != null && x.Role.IsActive)
            .Join(_dbContext.SysRolePermissions, userRole => userRole.RoleId, permission => permission.RoleId, (_, permission) => permission.MenuId)
            .Join(_dbContext.SysMenus.Where(x => x.IsActive), menuId => menuId, menu => menu.Id, (menuId, _) => menuId)
            .Distinct()
            .ToListAsync();
    }

    private async Task<List<Guid>> GetPostPermissionMenuIdsAsync(Guid? postId)
    {
        if (!postId.HasValue || !await _dbContext.SysPosts.AnyAsync(x => x.Id == postId.Value && x.IsActive))
        {
            return new List<Guid>();
        }

        return await _dbContext.SysPostPermissions
            .Where(x => x.PostId == postId.Value)
            .Join(_dbContext.SysMenus.Where(x => x.IsActive), permission => permission.MenuId, menu => menu.Id, (permission, _) => permission.MenuId)
            .Distinct()
            .ToListAsync();
    }

    private async Task<List<string>> GetPermissionCodesByMenuIdsAsync(List<Guid> menuIds)
    {
        if (menuIds.Count == 0)
        {
            return new List<string>();
        }

        return await _dbContext.SysMenus
            .Where(x => x.IsActive && menuIds.Contains(x.Id) && !string.IsNullOrWhiteSpace(x.PermissionCode))
            .Select(x => x.PermissionCode!)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();
    }

    private static List<Guid> ExpandWithAncestors(List<SysMenu> allMenus, List<Guid> selectedIds)
    {
        var map = allMenus.ToDictionary(x => x.Id, x => x);
        var result = new HashSet<Guid>(selectedIds);

        foreach (var id in selectedIds)
        {
            var currentId = id;
            while (map.TryGetValue(currentId, out var menu) && menu.ParentId.HasValue)
            {
                result.Add(menu.ParentId.Value);
                currentId = menu.ParentId.Value;
            }
        }

        return result.ToList();
    }

    private static HashSet<Guid> CollectDescendantIds(List<SysMenu> allMenus, Guid rootId)
    {
        var childLookup = allMenus
            .Where(x => x.ParentId.HasValue)
            .GroupBy(x => x.ParentId!.Value)
            .ToDictionary(x => x.Key, x => x.Select(m => m.Id).ToList());
        var result = new HashSet<Guid>();
        var queue = new Queue<Guid>();
        queue.Enqueue(rootId);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!result.Add(current))
            {
                continue;
            }

            if (childLookup.TryGetValue(current, out var children))
            {
                foreach (var childId in children)
                {
                    queue.Enqueue(childId);
                }
            }
        }

        return result;
    }

    private static List<MenuTreeNodeDto> BuildMenuTree(List<SysMenu> menus)
    {
        var nodes = menus.Select(MapMenuTreeNode).ToDictionary(x => x.Id, x => x);
        var roots = new List<MenuTreeNodeDto>();

        foreach (var menu in menus.OrderBy(x => x.SortOrder).ThenBy(x => x.CreatedAt))
        {
            var current = nodes[menu.Id];
            if (menu.ParentId.HasValue && nodes.TryGetValue(menu.ParentId.Value, out var parent))
            {
                parent.Children.Add(current);
            }
            else
            {
                roots.Add(current);
            }
        }

        return roots.OrderBy(x => x.SortOrder).ToList();
    }

    private static MenuTreeNodeDto MapMenuTreeNode(SysMenu menu)
    {
        return new MenuTreeNodeDto
        {
            Id = menu.Id,
            ParentId = menu.ParentId,
            MenuKey = menu.MenuKey,
            MenuName = menu.MenuName,
            MenuType = menu.MenuType,
            SortOrder = menu.SortOrder,
            RoutePath = menu.RoutePath,
            ComponentPath = menu.ComponentPath,
            Icon = menu.Icon,
            IsVisible = menu.IsVisible,
            IsActive = menu.IsActive,
            PermissionCode = menu.PermissionCode
        };
    }

    private static MenuDto MapMenu(SysMenu menu)
    {
        return new MenuDto
        {
            Id = menu.Id,
            ParentId = menu.ParentId,
            MenuKey = menu.MenuKey,
            MenuName = menu.MenuName,
            MenuType = menu.MenuType,
            SortOrder = menu.SortOrder,
            RoutePath = menu.RoutePath,
            ComponentPath = menu.ComponentPath,
            Icon = menu.Icon,
            IsVisible = menu.IsVisible,
            IsActive = menu.IsActive,
            PermissionCode = menu.PermissionCode
        };
    }

    private static UserProfileDto MapUserProfile(SysUser user)
    {
        return new UserProfileDto
        {
            Id = user.Id,
            Username = user.Username,
            Name = user.Name,
            Phone = user.Phone,
            Email = user.Email,
            PostId = user.PostId,
            PostName = user.Post?.PostName,
            IsAdmin = user.IsAdmin,
            IsActive = user.IsActive,
            RoleIds = user.UserRoles.Select(x => x.RoleId).ToList(),
            RoleNames = user.UserRoles.Where(x => x.Role != null).Select(x => x.Role!.RoleName).ToList()
        };
    }

    private static UserDto MapUser(SysUser user)
    {
        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Name = user.Name,
            Phone = user.Phone,
            Email = user.Email,
            EmployeeId = user.EmployeeId,
            PostId = user.PostId,
            PostName = user.Post?.PostName,
            IsActive = user.IsActive,
            IsAdmin = user.IsAdmin,
            RoleIds = user.UserRoles.Select(x => x.RoleId).ToList(),
            RoleNames = user.UserRoles.Where(x => x.Role != null).Select(x => x.Role!.RoleName).ToList()
        };
    }
}
