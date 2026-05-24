using HRMS.Domain.Enums;

namespace HRMS.Domain.Entities;

public class OrgUnit
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public OrgLevel Level { get; set; }
    public Guid? ParentId { get; set; }
    public OrgUnit? Parent { get; set; }
    public ICollection<OrgUnit> Children { get; set; } = new List<OrgUnit>();
    public string? ManagerId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
