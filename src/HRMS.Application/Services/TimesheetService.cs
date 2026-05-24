using HRMS.Application.DTOs;
using HRMS.Domain.Entities;
using HRMS.Domain.Enums;
using HRMS.Domain.Interfaces;

namespace HRMS.Application.Services;

public class TimesheetService
{
    private readonly ITimesheetRepository _timesheetRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IOrgUnitRepository _orgUnitRepository;

    public TimesheetService(
        ITimesheetRepository timesheetRepository,
        IEmployeeRepository employeeRepository,
        IOrgUnitRepository orgUnitRepository)
    {
        _timesheetRepository = timesheetRepository;
        _employeeRepository = employeeRepository;
        _orgUnitRepository = orgUnitRepository;
    }

    public async Task<IEnumerable<TimesheetDto>> GetAllAsync()
    {
        var timesheets = await _timesheetRepository.GetAllAsync();
        return timesheets.Select(MapToDto);
    }

    public async Task<TimesheetDto?> GetByIdAsync(Guid id)
    {
        var timesheet = await _timesheetRepository.GetByIdAsync(id);
        return timesheet == null ? null : MapToDto(timesheet);
    }

    public async Task<IEnumerable<TimesheetDto>> GetByEmployeeAndMonthAsync(Guid employeeId, int year, int month)
    {
        var timesheets = await _timesheetRepository.GetByEmployeeAndMonthAsync(employeeId, year, month);
        return timesheets.Select(MapToDto);
    }

    public async Task<IEnumerable<TimesheetDto>> GetByOrgUnitAndDateRangeAsync(Guid orgUnitId, DateTime startDate, DateTime endDate)
    {
        var timesheets = await _timesheetRepository.GetByOrgUnitAndDateRangeAsync(orgUnitId, startDate, endDate);
        return timesheets.Select(MapToDto);
    }

    public async Task<TimesheetDto> CreateAsync(CreateTimesheetDto dto)
    {
        var orgUnit = await _orgUnitRepository.GetByIdAsync(dto.ActualOrgUnitId);
        if (orgUnit == null) throw new Exception("组织单元不存在");

        var timesheet = new Timesheet
        {
            EmployeeId = dto.EmployeeId,
            Date = dto.Date,
            ActualOrgUnitId = dto.ActualOrgUnitId,
            WorkingHours = dto.WorkingHours,
            OvertimeHours = CalculateOvertimeHours(dto.WorkingHours, 8.0m),
            ShiftType = (WorkShiftType)dto.ShiftType,
            Remark = dto.Remark
        };

        var created = await _timesheetRepository.CreateAsync(timesheet);
        return MapToDto(created);
    }

    public async Task<int> ImportBatchAsync(List<ImportTimesheetDto> dtos)
    {
        var employeeNoDict = new Dictionary<string, Guid>();
        var orgCodeDict = new Dictionary<string, Guid>();
        var timesheets = new List<Timesheet>();

        foreach (var dto in dtos)
        {
            if (!employeeNoDict.TryGetValue(dto.EmployeeNo, out var employeeId))
            {
                var employee = await _employeeRepository.GetByEmployeeNoAsync(dto.EmployeeNo);
                if (employee == null) continue;
                employeeId = employee.Id;
                employeeNoDict[dto.EmployeeNo] = employeeId;
            }

            if (!orgCodeDict.TryGetValue(dto.ActualOrgUnitCode, out var orgUnitId))
            {
                var orgUnit = await _orgUnitRepository.GetByCodeAsync(dto.ActualOrgUnitCode);
                if (orgUnit == null) continue;
                orgUnitId = orgUnit.Id;
                orgCodeDict[dto.ActualOrgUnitCode] = orgUnitId;
            }

            var orgUnitEntity = await _orgUnitRepository.GetByIdAsync(orgUnitId);
            if (orgUnitEntity == null) continue;

            timesheets.Add(new Timesheet
            {
                EmployeeId = employeeId,
                Date = dto.Date,
                ActualOrgUnitId = orgUnitId,
                WorkingHours = dto.WorkingHours,
                OvertimeHours = CalculateOvertimeHours(dto.WorkingHours, 8.0m),
                ShiftType = (WorkShiftType)dto.ShiftType,
                Remark = dto.Remark
            });
        }

        if (timesheets.Any())
        {
            await _timesheetRepository.CreateRangeAsync(timesheets);
        }

        return timesheets.Count;
    }

    public async Task<bool> ApproveAsync(Guid id, Guid approverId)
    {
        var timesheet = await _timesheetRepository.GetByIdAsync(id);
        if (timesheet == null) return false;

        timesheet.ApprovalStatus = ApprovalStatus.Approved;
        timesheet.ApproverId = approverId;
        timesheet.ApprovedAt = DateTime.UtcNow;

        await _timesheetRepository.UpdateAsync(timesheet);
        return true;
    }

    private static decimal CalculateOvertimeHours(decimal workingHours, decimal standardDailyHours)
    {
        return workingHours > standardDailyHours ? workingHours - standardDailyHours : 0;
    }

    private static TimesheetDto MapToDto(Timesheet timesheet)
    {
        return new TimesheetDto
        {
            Id = timesheet.Id,
            EmployeeId = timesheet.EmployeeId,
            EmployeeName = timesheet.Employee?.Name ?? string.Empty,
            EmployeeNo = timesheet.Employee?.EmployeeNo ?? string.Empty,
            Date = timesheet.Date,
            ActualOrgUnitId = timesheet.ActualOrgUnitId,
            ActualOrgUnitName = timesheet.ActualOrgUnit?.Name ?? string.Empty,
            WorkingHours = timesheet.WorkingHours,
            OvertimeHours = timesheet.OvertimeHours,
            ShiftType = timesheet.ShiftType.ToString(),
            Remark = timesheet.Remark,
            ApprovalStatus = timesheet.ApprovalStatus.ToString(),
            CreatedAt = timesheet.CreatedAt
        };
    }
}