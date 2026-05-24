using HRMS.Domain.Enums;

namespace HRMS.Domain.Entities;

public class ExpenseApplication
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ApplicationNo { get; set; } = string.Empty;
    public ExpenseType ExpenseType { get; set; }
    public Guid ApplicantId { get; set; }
    public Guid OrgUnitId { get; set; }
    public OrgUnit? OrgUnit { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public ExpenseStatus Status { get; set; } = ExpenseStatus.Draft;
    public Guid? ApproverId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovalComment { get; set; }
    public string? AttachmentUrls { get; set; }
    public Guid? BudgetId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
