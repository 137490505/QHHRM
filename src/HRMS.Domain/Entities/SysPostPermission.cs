namespace HRMS.Domain.Entities;

public class SysPostPermission
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PostId { get; set; }
    public SysPost? Post { get; set; }
    public Guid MenuId { get; set; }
    public SysMenu? Menu { get; set; }
    public string PermissionCode { get; set; } = string.Empty;
    public string PermissionType { get; set; } = string.Empty;
}
