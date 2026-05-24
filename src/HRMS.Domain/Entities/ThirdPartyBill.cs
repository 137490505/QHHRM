using HRMS.Domain.Enums;

namespace HRMS.Domain.Entities;

public class ThirdPartyBill
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string BillNo { get; set; } = string.Empty;
    public Guid ClientId { get; set; }
    public OrgUnit? Client { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal TotalWages { get; set; }
    public decimal ManagementFee { get; set; }
    public decimal ManagementFeeRate { get; set; }
    public string ManagementFeeType { get; set; } = "Fixed";
    public decimal OtherFees { get; set; }
    public decimal TotalAmount { get; set; }
    public BillStatus Status { get; set; } = BillStatus.Draft;
    public DateTime? ConfirmedAt { get; set; }
    public string? InvoiceNo { get; set; }
    public DateTime? InvoicedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? Remark { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public ICollection<ThirdPartyBillDetail> Details { get; set; } = new List<ThirdPartyBillDetail>();
}
