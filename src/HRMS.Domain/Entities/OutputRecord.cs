namespace HRMS.Domain.Entities;

public class OutputRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    public DateTime Date { get; set; }
    public Guid OrgUnitId { get; set; }
    public OrgUnit? OrgUnit { get; set; }
    public decimal QualifiedQuantity { get; set; }
    public bool IsConfirmedByClient { get; set; } = false;
    public DateTime? ConfirmedAt { get; set; }
    public string? Remark { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
