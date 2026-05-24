namespace HRMS.Domain.Entities;

public class SysPost
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string PostCode { get; set; } = string.Empty;
    public string PostName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public ICollection<SysPostPermission> Permissions { get; set; } = new List<SysPostPermission>();
    public ICollection<SysUser> Users { get; set; } = new List<SysUser>();
}
