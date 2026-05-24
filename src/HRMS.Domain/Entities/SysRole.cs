namespace HRMS.Domain.Entities;

public class SysRole
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string RoleCode { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? OrgUnitId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public ICollection<SysRolePermission> Permissions { get; set; } = new List<SysRolePermission>();
    public ICollection<SysUserRole> UserRoles { get; set; } = new List<SysUserRole>();
}
