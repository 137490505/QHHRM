using HRMS.Application.DTOs;
using HRMS.Domain.Entities;
using HRMS.Domain.Interfaces;

namespace HRMS.Application.Services;

public class EmployeeTypeService
{
    private readonly IEmployeeTypeRepository _employeeTypeRepository;

    public EmployeeTypeService(IEmployeeTypeRepository employeeTypeRepository)
    {
        _employeeTypeRepository = employeeTypeRepository;
    }

    public async Task<IEnumerable<PayrollEmployeeTypeDto>> GetAllAsync()
    {
        var entities = await _employeeTypeRepository.GetAllAsync();
        return entities.Select(MapToDto);
    }

    public async Task<PayrollEmployeeTypeDto?> GetByIdAsync(Guid id)
    {
        var entity = await _employeeTypeRepository.GetByIdAsync(id);
        return entity == null ? null : MapToDto(entity);
    }

    public async Task<PayrollEmployeeTypeDto> CreateAsync(CreatePayrollEmployeeTypeDto dto)
    {
        var existing = await _employeeTypeRepository.GetByCodeAsync(dto.TypeCode);
        if (existing != null)
        {
            throw new InvalidOperationException("员工类型编码已存在");
        }

        var entity = new PayrollEmployeeType
        {
            TypeCode = dto.TypeCode.Trim(),
            TypeName = dto.TypeName.Trim(),
            SalaryMode = dto.SalaryMode.Trim(),
            HasOvertime = dto.HasOvertime,
            HasMealSubsidy = dto.HasMealSubsidy,
            HasNightSubsidy = dto.HasNightSubsidy,
            HasPerformance = dto.HasPerformance,
            HasSocialSecurity = dto.HasSocialSecurity,
            IsActive = dto.IsActive,
            SortOrder = dto.SortOrder,
            Remark = dto.Remark
        };

        var created = await _employeeTypeRepository.CreateAsync(entity);
        return MapToDto(created);
    }

    public async Task<PayrollEmployeeTypeDto?> UpdateAsync(UpdatePayrollEmployeeTypeDto dto)
    {
        var entity = await _employeeTypeRepository.GetByIdAsync(dto.Id);
        if (entity == null)
        {
            return null;
        }

        if (!string.Equals(entity.TypeCode, dto.TypeCode, StringComparison.OrdinalIgnoreCase))
        {
            var existing = await _employeeTypeRepository.GetByCodeAsync(dto.TypeCode);
            if (existing != null && existing.Id != dto.Id)
            {
                throw new InvalidOperationException("员工类型编码已存在");
            }
        }

        entity.TypeCode = dto.TypeCode.Trim();
        entity.TypeName = dto.TypeName.Trim();
        entity.SalaryMode = dto.SalaryMode.Trim();
        entity.HasOvertime = dto.HasOvertime;
        entity.HasMealSubsidy = dto.HasMealSubsidy;
        entity.HasNightSubsidy = dto.HasNightSubsidy;
        entity.HasPerformance = dto.HasPerformance;
        entity.HasSocialSecurity = dto.HasSocialSecurity;
        entity.IsActive = dto.IsActive;
        entity.SortOrder = dto.SortOrder;
        entity.Remark = dto.Remark;

        await _employeeTypeRepository.UpdateAsync(entity);
        return MapToDto(entity);
    }

    private static PayrollEmployeeTypeDto MapToDto(PayrollEmployeeType entity)
    {
        return new PayrollEmployeeTypeDto
        {
            Id = entity.Id,
            TypeCode = entity.TypeCode,
            TypeName = entity.TypeName,
            SalaryMode = entity.SalaryMode,
            HasOvertime = entity.HasOvertime,
            HasMealSubsidy = entity.HasMealSubsidy,
            HasNightSubsidy = entity.HasNightSubsidy,
            HasPerformance = entity.HasPerformance,
            HasSocialSecurity = entity.HasSocialSecurity,
            IsActive = entity.IsActive,
            SortOrder = entity.SortOrder,
            Remark = entity.Remark
        };
    }
}
