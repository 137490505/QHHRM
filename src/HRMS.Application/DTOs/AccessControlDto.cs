using System.ComponentModel.DataAnnotations;

namespace HRMS.Application.DTOs;

public class LoginRequestDto
{
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? Captcha { get; set; }
}

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public UserProfileDto UserInfo { get; set; } = new();
    public List<string> Permissions { get; set; } = new();
    public List<MenuTreeNodeDto> Menus { get; set; } = new();
}

public class UserProfileDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public Guid? PostId { get; set; }
    public string? PostName { get; set; }
    public bool IsAdmin { get; set; }
    public bool IsActive { get; set; }
    public List<Guid> RoleIds { get; set; } = new();
    public List<string> RoleNames { get; set; } = new();
}

public class MenuTreeNodeDto
{
    public Guid Id { get; set; }
    public Guid? ParentId { get; set; }
    public string MenuKey { get; set; } = string.Empty;
    public string MenuName { get; set; } = string.Empty;
    public string MenuType { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public string? RoutePath { get; set; }
    public string? ComponentPath { get; set; }
    public string? Icon { get; set; }
    public bool IsVisible { get; set; }
    public bool IsActive { get; set; }
    public string? PermissionCode { get; set; }
    public List<MenuTreeNodeDto> Children { get; set; } = new();
}

public class AccessOptionItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class RoleDto
{
    public Guid Id { get; set; }
    public string RoleCode { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int UserCount { get; set; }
    public List<Guid> PermissionMenuIds { get; set; } = new();
}

public class CreateRoleDto
{
    [Required(ErrorMessage = "角色编码不能为空")]
    public string RoleCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "角色名称不能为空")]
    public string RoleName { get; set; } = string.Empty;

    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateRoleDto : CreateRoleDto
{
    public Guid Id { get; set; }
}

public class PostDto
{
    public Guid Id { get; set; }
    public string PostCode { get; set; } = string.Empty;
    public string PostName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int UserCount { get; set; }
    public List<Guid> PermissionMenuIds { get; set; } = new();
}

public class CreatePostDto
{
    [Required(ErrorMessage = "岗位编码不能为空")]
    public string PostCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "岗位名称不能为空")]
    public string PostName { get; set; } = string.Empty;

    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdatePostDto : CreatePostDto
{
    public Guid Id { get; set; }
}

public class MenuDto
{
    public Guid Id { get; set; }
    public Guid? ParentId { get; set; }
    public string MenuKey { get; set; } = string.Empty;
    public string MenuName { get; set; } = string.Empty;
    public string MenuType { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public string? RoutePath { get; set; }
    public string? ComponentPath { get; set; }
    public string? Icon { get; set; }
    public bool IsVisible { get; set; }
    public bool IsActive { get; set; }
    public string? PermissionCode { get; set; }
}

public class CreateMenuDto
{
    public Guid? ParentId { get; set; }

    [Required(ErrorMessage = "菜单Key不能为空")]
    public string MenuKey { get; set; } = string.Empty;

    [Required(ErrorMessage = "菜单名称不能为空")]
    public string MenuName { get; set; } = string.Empty;

    [Required(ErrorMessage = "菜单类型不能为空")]
    public string MenuType { get; set; } = string.Empty;

    public int SortOrder { get; set; }
    public string? RoutePath { get; set; }
    public string? ComponentPath { get; set; }
    public string? Icon { get; set; }
    public bool IsVisible { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public string? PermissionCode { get; set; }
}

public class UpdateMenuDto : CreateMenuDto
{
    public Guid Id { get; set; }
}

public class UserDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public Guid? EmployeeId { get; set; }
    public Guid? PostId { get; set; }
    public string? PostName { get; set; }
    public bool IsActive { get; set; }
    public bool IsAdmin { get; set; }
    public List<Guid> RoleIds { get; set; } = new();
    public List<string> RoleNames { get; set; } = new();
}

public class UserPermissionContextDto
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid? PostId { get; set; }
    public string? PostName { get; set; }
    public bool IsAdmin { get; set; }
    public List<Guid> RoleIds { get; set; } = new();
    public List<string> RoleNames { get; set; } = new();
    public List<string> PermissionCodes { get; set; } = new();
    public List<Guid> GrantedMenuIds { get; set; } = new();
    public List<Guid> RolePermissionMenuIds { get; set; } = new();
    public List<Guid> PostPermissionMenuIds { get; set; } = new();
    public List<MenuTreeNodeDto> MenuTree { get; set; } = new();
}

public class CreateUserDto
{
    [Required(ErrorMessage = "用户名不能为空")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "密码不能为空")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "姓名不能为空")]
    public string Name { get; set; } = string.Empty;

    public string? Phone { get; set; }
    public string? Email { get; set; }
    public Guid? EmployeeId { get; set; }
    public Guid? PostId { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsAdmin { get; set; }
    public List<Guid> RoleIds { get; set; } = new();
}

public class UpdateUserDto
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "用户名不能为空")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "姓名不能为空")]
    public string Name { get; set; } = string.Empty;

    public string? Password { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public Guid? EmployeeId { get; set; }
    public Guid? PostId { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsAdmin { get; set; }
    public List<Guid> RoleIds { get; set; } = new();
}

public class PermissionAssignmentDto
{
    public List<Guid> MenuIds { get; set; } = new();
}

public class CopyPermissionDto
{
    public Guid SourceId { get; set; }
}
