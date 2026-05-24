using HRMS.Application.DTOs;
using HRMS.Domain.Entities;
using HRMS.Domain.Interfaces;

namespace HRMS.Application.Services;

public class EmployeePayrollProfileService
{
    private readonly IEmployeePayrollProfileRepository _profileRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IEmployeeTypeRepository _employeeTypeRepository;

    public EmployeePayrollProfileService(
        IEmployeePayrollProfileRepository profileRepository,
        IEmployeeRepository employeeRepository,
        IEmployeeTypeRepository employeeTypeRepository)
    {
        _profileRepository = profileRepository;
        _employeeRepository = employeeRepository;
        _employeeTypeRepository = employeeTypeRepository;
    }

    public async Task<IEnumerable<EmployeePayrollProfileDto>> GetAllAsync()
    {
        var entities = await _profileRepository.GetAllAsync();
        return entities.Select(MapToDto);
    }

    public async Task<EmployeePayrollProfileDto?> GetByIdAsync(Guid id)
    {
        var entity = await _profileRepository.GetByIdAsync(id);
        return entity == null ? null : MapToDto(entity);
    }

    public async Task<EmployeePayrollProfileDto?> GetByEmployeeIdAsync(Guid employeeId)
    {
        var entity = await _profileRepository.GetByEmployeeIdAsync(employeeId);
        return entity == null ? null : MapToDto(entity);
    }

    public async Task<EmployeePayrollProfileDto> CreateAsync(CreateEmployeePayrollProfileDto dto)
    {
        var employee = await _employeeRepository.GetByIdAsync(dto.EmployeeId)
            ?? throw new InvalidOperationException("员工不存在");
        var employeeType = await _employeeTypeRepository.GetByIdAsync(dto.EmployeeTypeId)
            ?? throw new InvalidOperationException("员工类型不存在");

        var exists = await _profileRepository.GetByEmployeeIdAsync(dto.EmployeeId);
        if (exists != null)
        {
            throw new InvalidOperationException("该员工已存在薪资档案");
        }

        var entity = new EmployeePayrollProfile
        {
            EmployeeId = dto.EmployeeId,
            EmployeeTypeId = dto.EmployeeTypeId,
            ShiftType = dto.ShiftType,
            BankAccount = dto.BankAccount,
            PayrollStatus = dto.PayrollStatus,
            JoinPayrollDate = dto.JoinPayrollDate?.Date,
            LeavePayrollDate = dto.LeavePayrollDate?.Date,
            Remark = dto.Remark
        };

        var created = await _profileRepository.CreateAsync(entity);
        created.Employee = employee;
        created.EmployeeType = employeeType;
        return MapToDto(created);
    }

    public async Task<EmployeePayrollProfileDto?> UpdateAsync(UpdateEmployeePayrollProfileDto dto)
    {
        var entity = await _profileRepository.GetByIdAsync(dto.Id);
        if (entity == null)
        {
            return null;
        }

        var employee = await _employeeRepository.GetByIdAsync(dto.EmployeeId)
            ?? throw new InvalidOperationException("员工不存在");
        var employeeType = await _employeeTypeRepository.GetByIdAsync(dto.EmployeeTypeId)
            ?? throw new InvalidOperationException("员工类型不存在");

        var exists = await _profileRepository.GetByEmployeeIdAsync(dto.EmployeeId);
        if (exists != null && exists.Id != dto.Id)
        {
            throw new InvalidOperationException("该员工已存在其他薪资档案");
        }

        entity.EmployeeId = dto.EmployeeId;
        entity.EmployeeTypeId = dto.EmployeeTypeId;
        entity.ShiftType = dto.ShiftType;
        entity.BankAccount = dto.BankAccount;
        entity.PayrollStatus = dto.PayrollStatus;
        entity.JoinPayrollDate = dto.JoinPayrollDate?.Date;
        entity.LeavePayrollDate = dto.LeavePayrollDate?.Date;
        entity.Remark = dto.Remark;

        await _profileRepository.UpdateAsync(entity);
        entity.Employee = employee;
        entity.EmployeeType = employeeType;
        return MapToDto(entity);
    }

    private static EmployeePayrollProfileDto MapToDto(EmployeePayrollProfile entity)
    {
        return new EmployeePayrollProfileDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeNo = entity.Employee?.EmployeeNo ?? string.Empty,
            EmployeeName = entity.Employee?.Name ?? string.Empty,
            EmployeeTypeId = entity.EmployeeTypeId,
            EmployeeTypeName = entity.EmployeeType?.TypeName ?? string.Empty,
            ShiftType = entity.ShiftType,
            BankAccount = entity.BankAccount,
            PayrollStatus = entity.PayrollStatus,
            JoinPayrollDate = entity.JoinPayrollDate,
            LeavePayrollDate = entity.LeavePayrollDate,
            Remark = entity.Remark,
            CreatedAt = entity.CreatedAt
        };
    }
}
