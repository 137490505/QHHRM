namespace HRMS.Application.DTOs;

public class SalaryCalculationDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeNo { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal RegularHours { get; set; }
    public decimal OvertimeHours { get; set; }
    public decimal RegularWages { get; set; }
    public decimal OvertimeWages { get; set; }
    public decimal MealAllowance { get; set; }
    public decimal Benefits { get; set; }
    public decimal PerformanceBonus { get; set; }
    public decimal AbsenceDeduction { get; set; }
    public decimal OtherDeductions { get; set; }
    public decimal GrossWages { get; set; }
    public decimal SocialSecurityPersonal { get; set; }
    public decimal HousingFundPersonal { get; set; }
    public decimal IncomeTax { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal NetWages { get; set; }
    public DateTime CalculatedAt { get; set; }
}

public class CalculateSalaryDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public List<Guid>? EmployeeIds { get; set; }
    public List<Guid>? OrgUnitIds { get; set; }
}

public class SalaryConfigDto
{
    public int TrialDays { get; set; } = 3;
    public decimal TrialDailySalary { get; set; } = 80m;
    public bool TrialNoSalaryIfInsufficient { get; set; } = true;
    public bool TrialIgnoreOvertime { get; set; } = true;
    public decimal StandardDailyHours { get; set; } = 8.0m;
    public decimal WeekdayOvertimeMultiplier { get; set; } = 1.5m;
    public decimal WeekendOvertimeMultiplier { get; set; } = 2.0m;
    public decimal HolidayOvertimeMultiplier { get; set; } = 3.0m;
    public List<MealAllowanceTierDto> MealAllowanceTiers { get; set; } = new()
    {
        new MealAllowanceTierDto { MinHours = 0, MaxHours = 4, Amount = 0 },
        new MealAllowanceTierDto { MinHours = 4, MaxHours = 6, Amount = 10 },
        new MealAllowanceTierDto { MinHours = 6, MaxHours = 8, Amount = 15 },
        new MealAllowanceTierDto { MinHours = 8, MaxHours = decimal.MaxValue, Amount = 20 }
    };
}

public class MealAllowanceTierDto
{
    public decimal MinHours { get; set; }
    public decimal MaxHours { get; set; }
    public decimal Amount { get; set; }
}
