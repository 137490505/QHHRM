namespace HRMS.Application.DTOs;

public class TimesheetDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeNo { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public Guid ActualOrgUnitId { get; set; }
    public string ActualOrgUnitName { get; set; } = string.Empty;
    public decimal WorkingHours { get; set; }
    public decimal OvertimeHours { get; set; }
    public string ShiftType { get; set; } = string.Empty;
    public string? Remark { get; set; }
    public string ApprovalStatus { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CreateTimesheetDto
{
    public Guid EmployeeId { get; set; }
    public DateTime Date { get; set; }
    public Guid ActualOrgUnitId { get; set; }
    public decimal WorkingHours { get; set; }
    public int ShiftType { get; set; }
    public string? Remark { get; set; }
}

public class ImportTimesheetDto
{
    public string EmployeeNo { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string ActualOrgUnitCode { get; set; } = string.Empty;
    public decimal WorkingHours { get; set; }
    public int ShiftType { get; set; }
    public string? Remark { get; set; }
}
