namespace HRMS.Domain.Entities;

public class ThirdPartyBillDetail
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ThirdPartyBillId { get; set; }
    public ThirdPartyBill? ThirdPartyBill { get; set; }
    public string Tag { get; set; } = string.Empty;
    public decimal TotalWages { get; set; }
    public decimal TotalManagementFee { get; set; }
    public int EmployeeCount { get; set; }
    public decimal TotalHours { get; set; }
}
