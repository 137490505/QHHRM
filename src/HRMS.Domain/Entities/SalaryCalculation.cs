namespace HRMS.Domain.Entities;

public class SalaryCalculation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
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
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
}
