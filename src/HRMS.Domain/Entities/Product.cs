namespace HRMS.Domain.Entities;

public class Product
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Unit { get; set; }
    public decimal ClientUnitPrice { get; set; }
    public DateTime? ClientPriceEffectiveFrom { get; set; }
    public DateTime? ClientPriceEffectiveTo { get; set; }
    public decimal EmployeeUnitPrice { get; set; }
    public DateTime? EmployeePriceEffectiveFrom { get; set; }
    public DateTime? EmployeePriceEffectiveTo { get; set; }
    public Guid OrgUnitId { get; set; }
    public OrgUnit? OrgUnit { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
