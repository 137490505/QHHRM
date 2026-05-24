using System.ComponentModel.DataAnnotations;

namespace HRMS.Application.DTOs;

public class EmployeeDto
{
    public Guid Id { get; set; }
    public string EmployeeNo { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Gender { get; set; }
    public string IdCard { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string EmployeeType { get; set; } = string.Empty;
    public string SalaryMode { get; set; } = string.Empty;
    public Guid OrgUnitId { get; set; }
    public string OrgUnitName { get; set; } = string.Empty;
    public Guid? ThirdPartyCompanyId { get; set; }
    public string? ThirdPartyCompanyName { get; set; }
    public string? JobTitle { get; set; }
    public string? Level { get; set; }
    public List<string> Tags { get; set; } = new();
    public decimal? HourlyRate { get; set; }
    public decimal? MonthlySalary { get; set; }
    public decimal? PieceRatePrice { get; set; }
    public decimal? SocialSecurityBase { get; set; }
    public decimal? HousingFundBase { get; set; }
    public DateTime? TrialEndDate { get; set; }
    public int TrialDaysRemaining { get; set; }
    public int ProbationDays { get; set; }
    public int ContractType { get; set; }
    public DateTime? ContractStartDate { get; set; }
    public DateTime? ContractEndDate { get; set; }
    public DateTime? HireDate { get; set; }
    public bool IsBlacklisted { get; set; }
    public bool IsActive { get; set; }
    public DateTime? DismissDate { get; set; }
    public string? DismissReason { get; set; }
}

public class CreateEmployeeDto
{
    [Required(ErrorMessage = "工号不能为空")]
    [RegularExpression(@"^\d+$", ErrorMessage = "工号必须为全数字")]
    public string EmployeeNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "姓名不能为空")]
    public string Name { get; set; } = string.Empty;

    public int Gender { get; set; }

    [Required(ErrorMessage = "身份证号不能为空")]
    [RegularExpression(@"^[1-9]\d{5}(18|19|20)\d{2}(0[1-9]|1[0-2])(0[1-9]|[12]\d|3[01])\d{3}[\dXx]$", ErrorMessage = "身份证号格式不正确（应为18位）")]
    public string IdCard { get; set; } = string.Empty;

    [Required(ErrorMessage = "手机号不能为空")]
    [RegularExpression(@"^1[3-9]\d{9}$", ErrorMessage = "手机号格式不正确（应为11位数字）")]
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public int EmployeeType { get; set; }
    public int SalaryMode { get; set; }
    public Guid OrgUnitId { get; set; }
    public Guid? ThirdPartyCompanyId { get; set; }
    public string? JobTitle { get; set; }
    public string? Level { get; set; }
    public List<string>? Tags { get; set; }
    public decimal? HourlyRate { get; set; }
    public decimal? MonthlySalary { get; set; }
    public decimal? PieceRatePrice { get; set; }
    public decimal? SocialSecurityBase { get; set; }
    public decimal? HousingFundBase { get; set; }
    public int ContractType { get; set; }
    public DateTime? ContractStartDate { get; set; }
    public DateTime? ContractEndDate { get; set; }
    public int ProbationDays { get; set; } = 30;
    public DateTime? HireDate { get; set; }
}

public class UpdateEmployeeDto
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "姓名不能为空")]
    public string Name { get; set; } = string.Empty;

    public int Gender { get; set; }

    [Required(ErrorMessage = "身份证号不能为空")]
    [RegularExpression(@"^[1-9]\d{5}(18|19|20)\d{2}(0[1-9]|1[0-2])(0[1-9]|[12]\d|3[01])\d{3}[\dXx]$", ErrorMessage = "身份证号格式不正确（应为18位）")]
    public string IdCard { get; set; } = string.Empty;

    [Required(ErrorMessage = "手机号不能为空")]
    [RegularExpression(@"^1[3-9]\d{9}$", ErrorMessage = "手机号格式不正确（应为11位数字）")]
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public int EmployeeType { get; set; }
    public int SalaryMode { get; set; }
    public Guid OrgUnitId { get; set; }
    public Guid? ThirdPartyCompanyId { get; set; }
    public string? JobTitle { get; set; }
    public string? Level { get; set; }
    public List<string>? Tags { get; set; }
    public decimal? HourlyRate { get; set; }
    public decimal? MonthlySalary { get; set; }
    public decimal? PieceRatePrice { get; set; }
    public decimal? SocialSecurityBase { get; set; }
    public decimal? HousingFundBase { get; set; }
    public int ContractType { get; set; }
    public DateTime? ContractStartDate { get; set; }
    public DateTime? ContractEndDate { get; set; }
    public int ProbationDays { get; set; } = 30;
    public DateTime? HireDate { get; set; }
}

public class BatchDismissDto
{
    public List<Guid> Ids { get; set; } = new();
    public DateTime DismissDate { get; set; }
    public string? Reason { get; set; }
}
