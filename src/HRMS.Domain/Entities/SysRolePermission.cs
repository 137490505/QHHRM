namespace HRMS.Domain.Entities;

public class SysRolePermission
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RoleId { get; set; }
    public SysRole? Role { get; set; }
    public Guid MenuId { get; set; }
    public SysMenu? Menu { get; set; }
    public string PermissionCode { get; set; } = string.Empty;
    public string PermissionType { get; set; } = string.Empty;
}
