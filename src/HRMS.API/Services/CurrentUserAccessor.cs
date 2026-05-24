using Microsoft.EntityFrameworkCore;
using HRMS.Infrastructure.Data;

namespace HRMS.API.Services;

public class CurrentUserAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly TokenSessionStore _sessionStore;
    private readonly HrmsDbContext _dbContext;

    public CurrentUserAccessor(
        IHttpContextAccessor httpContextAccessor,
        TokenSessionStore sessionStore,
        HrmsDbContext dbContext)
    {
        _httpContextAccessor = httpContextAccessor;
        _sessionStore = sessionStore;
        _dbContext = dbContext;
    }

    public string? GetToken()
    {
        var authorization = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(authorization))
        {
            return null;
        }

        const string bearerPrefix = "Bearer ";
        return authorization.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase)
            ? authorization[bearerPrefix.Length..].Trim()
            : authorization.Trim();
    }

    public bool TryGetSession(out UserSessionContext? session)
    {
        var token = GetToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            session = null;
            return false;
        }

        session = _sessionStore.GetAsync(token).GetAwaiter().GetResult();
        return session != null;
    }

    public Task<Guid?> GetUserIdAsync()
    {
        return Task.FromResult<Guid?>(TryGetSession(out var session) ? session!.UserId : null);
    }

    public async Task<string?> GetUserDisplayNameAsync()
    {
        var userId = await GetUserIdAsync();
        if (!userId.HasValue)
        {
            return null;
        }

        return await _dbContext.SysUsers
            .Where(x => x.Id == userId.Value && x.IsActive)
            .Select(x => x.Name)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> IsCurrentUserAsync(Guid userId)
    {
        var currentUserId = await GetUserIdAsync();
        return currentUserId.HasValue && currentUserId.Value == userId;
    }

    public async Task<bool> HasPermissionAsync(string permissionCode)
    {
        var token = GetToken();
        if (string.IsNullOrWhiteSpace(token) || !TryGetSession(out var session))
        {
            return false;
        }

        var user = await _dbContext.SysUsers
            .Include(x => x.UserRoles)
                .ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x => x.Id == session!.UserId && x.IsActive);
        if (user == null)
        {
            return false;
        }

        if (user.IsAdmin)
        {
            _sessionStore.SetAsync(token, new UserSessionContext(user.Id, true, new HashSet<string>(StringComparer.OrdinalIgnoreCase))).GetAwaiter().GetResult();
            return true;
        }

        var rolePermissionCodes = await _dbContext.SysUserRoles
            .Where(x => x.UserId == user.Id && x.Role != null && x.Role.IsActive)
            .Join(_dbContext.SysRolePermissions, userRole => userRole.RoleId, permission => permission.RoleId, (_, permission) => permission.MenuId)
            .Join(
                _dbContext.SysMenus.Where(x => x.IsActive && !string.IsNullOrWhiteSpace(x.PermissionCode)),
                menuId => menuId,
                menu => menu.Id,
                (_, menu) => menu.PermissionCode!)
            .Distinct()
            .ToListAsync();

        var postPermissionCodes = user.PostId.HasValue && await _dbContext.SysPosts.AnyAsync(x => x.Id == user.PostId.Value && x.IsActive)
            ? await _dbContext.SysPostPermissions
                .Where(x => x.PostId == user.PostId.Value)
                .Join(
                    _dbContext.SysMenus.Where(x => x.IsActive && !string.IsNullOrWhiteSpace(x.PermissionCode)),
                    permission => permission.MenuId,
                    menu => menu.Id,
                    (_, menu) => menu.PermissionCode!)
                .Distinct()
                .ToListAsync()
            : new List<string>();

        var permissions = rolePermissionCodes
            .Concat(postPermissionCodes)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        _sessionStore.SetAsync(token, new UserSessionContext(user.Id, false, permissions)).GetAwaiter().GetResult();
        return permissions.Contains(permissionCode);
    }

    public async Task<bool> UserExistsAsync()
    {
        var userId = await GetUserIdAsync();
        return userId.HasValue && await _dbContext.SysUsers.AnyAsync(x => x.Id == userId.Value && x.IsActive);
    }
}
