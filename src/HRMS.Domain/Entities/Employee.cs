using HRMS.Domain.Enums;

namespace HRMS.Domain.Entities;

public class Employee
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EmployeeNo { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Gender { get; set; }
    public string IdCard { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public EmployeeType EmployeeType { get; set; }
    public SalaryMode SalaryMode { get; set; }
    public Guid OrgUnitId { get; set; }
    public OrgUnit? OrgUnit { get; set; }
    public Guid? ThirdPartyCompanyId { get; set; }
    public OrgUnit? ThirdPartyCompany { get; set; }
    public string? JobTitle { get; set; }
    public string? Level { get; set; }
    public List<string> Tags { get; set; } = new();
    public decimal? HourlyRate { get; set; }
    public decimal? MonthlySalary { get; set; }
    public decimal? PieceRatePrice { get; set; }
    public decimal? SocialSecurityBase { get; set; }
    public decimal? HousingFundBase { get; set; }
    public DateTime? TrialEndDate { get; set; }
    public int TrialDaysRemaining { get; set; } = 3;
    public int ProbationDays { get; set; } = 30;
    public int ContractType { get; set; }
    public DateTime? ContractStartDate { get; set; }
    public DateTime? ContractEndDate { get; set; }
    public DateTime? HireDate { get; set; }
    public bool IsBlacklisted { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public DateTime? DismissDate { get; set; }
    public string? DismissReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
