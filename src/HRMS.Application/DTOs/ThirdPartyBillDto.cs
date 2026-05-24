namespace HRMS.Application.DTOs;

public class ThirdPartyBillDto
{
    public Guid Id { get; set; }
    public string BillNo { get; set; } = string.Empty;
    public Guid ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal TotalWages { get; set; }
    public decimal ManagementFee { get; set; }
    public string ManagementFeeType { get; set; } = string.Empty;
    public decimal OtherFees { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<ThirdPartyBillDetailDto> Details { get; set; } = new();
}

public class ThirdPartyBillDetailDto
{
    public string Tag { get; set; } = string.Empty;
    public decimal TotalWages { get; set; }
    public decimal TotalManagementFee { get; set; }
    public int EmployeeCount { get; set; }
    public decimal TotalHours { get; set; }
}

public class GenerateBillDto
{
    public Guid ClientId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public string ManagementFeeType { get; set; } = "Fixed";
    public decimal ManagementFeeRate { get; set; }
    public decimal OtherFees { get; set; }
}
