namespace HRMS.Domain.Entities;

public class Payslip
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SalaryCalculationId { get; set; }
    public SalaryCalculation? SalaryCalculation { get; set; }
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public string? PdfUrl { get; set; }
    public bool IsSigned { get; set; } = false;
    public DateTime? SignedAt { get; set; }
    public bool HasComplaint { get; set; } = false;
    public string? ComplaintReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
