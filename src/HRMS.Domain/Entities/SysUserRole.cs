namespace HRMS.Domain.Entities;

public class SysUserRole
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public SysUser? User { get; set; }
    public Guid RoleId { get; set; }
    public SysRole? Role { get; set; }
}
