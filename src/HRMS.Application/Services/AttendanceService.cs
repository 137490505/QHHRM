using HRMS.Application.DTOs;
using HRMS.Domain.Entities;
using HRMS.Domain.Interfaces;

namespace HRMS.Application.Services;

public class AttendanceService
{
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly IEmployeeRepository _employeeRepository;

    public AttendanceService(
        IAttendanceRepository attendanceRepository,
        IEmployeeRepository employeeRepository)
    {
        _attendanceRepository = attendanceRepository;
        _employeeRepository = employeeRepository;
    }

    public async Task<AttendanceDto?> GetByIdAsync(Guid id)
    {
        var entity = await _attendanceRepository.GetByIdAsync(id);
        return entity == null ? null : MapToDto(entity);
    }

    public async Task<IEnumerable<AttendanceDto>> GetByMonthAsync(int year, int month)
    {
        var entities = await _attendanceRepository.GetByMonthAsync(year, month);
        return entities.Select(MapToDto);
    }

    public async Task<IEnumerable<AttendanceDto>> GetByEmployeeAndMonthAsync(Guid employeeId, int year, int month)
    {
        var entities = await _attendanceRepository.GetByEmployeeAndMonthAsync(employeeId, year, month);
        return entities.Select(MapToDto);
    }

    public async Task<AttendanceDto> CreateAsync(CreateAttendanceDto dto)
    {
        var employee = await _employeeRepository.GetByIdAsync(dto.EmployeeId)
            ?? throw new InvalidOperationException("员工不存在");

        var existing = await _attendanceRepository.GetByEmployeeAndDateAsync(dto.EmployeeId, dto.WorkDate.Date);
        if (existing != null)
        {
            throw new InvalidOperationException("同一员工同一天的考勤记录已存在");
        }

        var entity = new Attendance
        {
            EmployeeId = dto.EmployeeId,
            WorkDate = dto.WorkDate.Date,
            NormalHours = dto.NormalHours,
            OvertimeHours = dto.OvertimeHours,
            OvertimeType = dto.OvertimeType,
            PieceworkQty = dto.PieceworkQty,
            ShiftType = dto.ShiftType,
            IsNightShift = dto.IsNightShift,
            AttendanceSource = dto.AttendanceSource,
            SourceRecordId = dto.SourceRecordId,
            Status = dto.Status,
            Remark = dto.Remark
        };

        var created = await _attendanceRepository.CreateAsync(entity);
        created.Employee = employee;
        return MapToDto(created);
    }

    public async Task<AttendanceDto?> UpdateAsync(UpdateAttendanceDto dto)
    {
        var entity = await _attendanceRepository.GetByIdAsync(dto.Id);
        if (entity == null)
        {
            return null;
        }

        var employee = await _employeeRepository.GetByIdAsync(dto.EmployeeId)
            ?? throw new InvalidOperationException("员工不存在");

        var existing = await _attendanceRepository.GetByEmployeeAndDateAsync(dto.EmployeeId, dto.WorkDate.Date);
        if (existing != null && existing.Id != dto.Id)
        {
            throw new InvalidOperationException("同一员工同一天的考勤记录已存在");
        }

        entity.EmployeeId = dto.EmployeeId;
        entity.WorkDate = dto.WorkDate.Date;
        entity.NormalHours = dto.NormalHours;
        entity.OvertimeHours = dto.OvertimeHours;
        entity.OvertimeType = dto.OvertimeType;
        entity.PieceworkQty = dto.PieceworkQty;
        entity.ShiftType = dto.ShiftType;
        entity.IsNightShift = dto.IsNightShift;
        entity.AttendanceSource = dto.AttendanceSource;
        entity.SourceRecordId = dto.SourceRecordId;
        entity.Status = dto.Status;
        entity.Remark = dto.Remark;

        await _attendanceRepository.UpdateAsync(entity);
        entity.Employee = employee;
        return MapToDto(entity);
    }

    private static AttendanceDto MapToDto(Attendance entity)
    {
        return new AttendanceDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeNo = entity.Employee?.EmployeeNo ?? string.Empty,
            EmployeeName = entity.Employee?.Name ?? string.Empty,
            WorkDate = entity.WorkDate,
            NormalHours = entity.NormalHours,
            OvertimeHours = entity.OvertimeHours,
            OvertimeType = entity.OvertimeType,
            PieceworkQty = entity.PieceworkQty,
            ShiftType = entity.ShiftType,
            IsNightShift = entity.IsNightShift,
            AttendanceSource = entity.AttendanceSource,
            SourceRecordId = entity.SourceRecordId,
            Status = entity.Status,
            Remark = entity.Remark,
            CreatedAt = entity.CreatedAt
        };
    }
}
