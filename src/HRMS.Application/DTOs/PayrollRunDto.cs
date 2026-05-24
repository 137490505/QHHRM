using System.ComponentModel.DataAnnotations;

namespace HRMS.Application.DTOs;

public class ExecutePayrollRunDto
{
    [Required(ErrorMessage = "年月不能为空")]
    [RegularExpression(@"^\d{4}-\d{2}$", ErrorMessage = "年月格式应为 YYYY-MM")]
    public string YearMonth { get; set; } = string.Empty;

    public List<Guid>? EmployeeIds { get; set; }
    public List<Guid>? OrgUnitIds { get; set; }
    public string? Remark { get; set; }
}

public class PayrollRunActionDto
{
    public string? Remark { get; set; }
}

public class SubmitPayrollApprovalDto
{
    public string? Remark { get; set; }
}

public class PayrollWorkflowCallbackDto
{
    [Required(ErrorMessage = "流程实例 ID 不能为空")]
    public string ProcessInstanceId { get; set; } = string.Empty;
    public string? OperatorId { get; set; }
    public string? OperatorName { get; set; }
    public string? Comment { get; set; }
}

public class PayrollRunDto
{
    public Guid Id { get; set; }
    public string RunNo { get; set; } = string.Empty;
    public string YearMonth { get; set; } = string.Empty;
    public string RunType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int PayrollCount { get; set; }
    public decimal TotalGross { get; set; }
    public decimal TotalNetSalary { get; set; }
    public string? ApprovalProcessCode { get; set; }
    public string? ApprovalProcessInstanceId { get; set; }
    public string? ApprovalRequestId { get; set; }
    public DateTime? ApprovalSubmittedAt { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? Remark { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public IReadOnlyList<PayrollEmployeeResultDto> Payrolls { get; set; } = [];
}

public class PayrollApprovalSubmissionResultDto
{
    public PayrollRunDto Run { get; set; } = new();
    public string ProcessInstanceId { get; set; } = string.Empty;
    public string ProcessCode { get; set; } = string.Empty;
    public string? CurrentNodeId { get; set; }
    public string? CurrentNodeName { get; set; }
    public string? CurrentAssigneeId { get; set; }
    public string? CurrentAssigneeName { get; set; }
}

public class PayrollEmployeeResultDto
{
    public Guid PayrollId { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeNo { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public Guid? EmployeeTypeId { get; set; }
    public string EmployeeTypeName { get; set; } = string.Empty;
    public string SalaryMode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal NormalWage { get; set; }
    public decimal OvertimeWage { get; set; }
    public decimal PieceworkWage { get; set; }
    public decimal FixedSalary { get; set; }
    public decimal BaseSalary { get; set; }
    public decimal MealSubsidy { get; set; }
    public decimal NightSubsidy { get; set; }
    public decimal PerformanceBonus { get; set; }
    public decimal OtherAllowance { get; set; }
    public decimal SocialSecurityEmployee { get; set; }
    public decimal SocialSecurityCompany { get; set; }
    public decimal ProvidentFundEmployee { get; set; }
    public decimal ProvidentFundCompany { get; set; }
    public decimal IncomeTax { get; set; }
    public decimal OtherDeduction { get; set; }
    public decimal TotalGross { get; set; }
    public decimal NetSalary { get; set; }
    public decimal TotalCompanyCost { get; set; }
    public DateTime? PaidAt { get; set; }
    public IReadOnlyList<PayrollDetailDto> Details { get; set; } = [];
}

public class PayrollDetailDto
{
    public Guid Id { get; set; }
    public string ComponentCode { get; set; } = string.Empty;
    public string ComponentName { get; set; } = string.Empty;
    public string ComponentCategory { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal? Quantity { get; set; }
    public decimal? UnitPrice { get; set; }
    public int SortOrder { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public string? SourceId { get; set; }
    public string? Remark { get; set; }
}
