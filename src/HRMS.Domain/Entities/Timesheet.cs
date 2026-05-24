using HRMS.Domain.Enums;

namespace HRMS.Domain.Entities;

public class Timesheet
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public DateTime Date { get; set; }
    public Guid ActualOrgUnitId { get; set; }
    public OrgUnit? ActualOrgUnit { get; set; }
    public decimal WorkingHours { get; set; }
    public decimal OvertimeHours { get; set; }
    public WorkShiftType ShiftType { get; set; } = WorkShiftType.Weekday;
    public string? Remark { get; set; }
    public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.Pending;
    public Guid? ApproverId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
