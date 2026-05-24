using HRMS.Application.DTOs;
using HRMS.Domain.Entities;
using HRMS.Domain.Interfaces;

namespace HRMS.Application.Services;

public class SalaryRuleService
{
    private readonly ISalaryRuleRepository _salaryRuleRepository;
    private readonly IEmployeeTypeRepository _employeeTypeRepository;

    public SalaryRuleService(
        ISalaryRuleRepository salaryRuleRepository,
        IEmployeeTypeRepository employeeTypeRepository)
    {
        _salaryRuleRepository = salaryRuleRepository;
        _employeeTypeRepository = employeeTypeRepository;
    }

    public async Task<IEnumerable<SalaryRuleDto>> GetAllAsync(Guid? employeeTypeId = null)
    {
        var entities = employeeTypeId.HasValue
            ? await _salaryRuleRepository.GetByEmployeeTypeAsync(employeeTypeId.Value)
            : await _salaryRuleRepository.GetAllAsync();

        return entities.Select(MapToDto);
    }

    public async Task<SalaryRuleDto?> GetByIdAsync(Guid id)
    {
        var entity = await _salaryRuleRepository.GetByIdAsync(id);
        return entity == null ? null : MapToDto(entity);
    }

    public async Task<SalaryRuleDto> CreateAsync(CreateSalaryRuleDto dto)
    {
        var employeeType = await _employeeTypeRepository.GetByIdAsync(dto.EmployeeTypeId);
        if (employeeType == null)
        {
            throw new InvalidOperationException("员工类型不存在");
        }

        var entity = new SalaryRule
        {
            RuleCode = dto.RuleCode.Trim(),
            RuleName = dto.RuleName.Trim(),
            EmployeeTypeId = dto.EmployeeTypeId,
            EffectiveStart = dto.EffectiveStart.Date,
            EffectiveEnd = dto.EffectiveEnd?.Date,
            FixedSalary = dto.FixedSalary,
            HourlyRate = dto.HourlyRate,
            PieceworkUnitPrice = dto.PieceworkUnitPrice,
            BaseSalaryForPiecework = dto.BaseSalaryForPiecework,
            MealSubsidyPerDay = dto.MealSubsidyPerDay,
            NightSubsidyPerDay = dto.NightSubsidyPerDay,
            PerformanceBase = dto.PerformanceBase,
            DefaultOvertimeMultiplier = dto.DefaultOvertimeMultiplier,
            IsActive = dto.IsActive,
            Remark = dto.Remark
        };

        var created = await _salaryRuleRepository.CreateAsync(entity);
        created.EmployeeType = employeeType;
        return MapToDto(created);
    }

    public async Task<SalaryRuleDto?> UpdateAsync(UpdateSalaryRuleDto dto)
    {
        var entity = await _salaryRuleRepository.GetByIdAsync(dto.Id);
        if (entity == null)
        {
            return null;
        }

        var employeeType = await _employeeTypeRepository.GetByIdAsync(dto.EmployeeTypeId);
        if (employeeType == null)
        {
            throw new InvalidOperationException("员工类型不存在");
        }

        entity.RuleCode = dto.RuleCode.Trim();
        entity.RuleName = dto.RuleName.Trim();
        entity.EmployeeTypeId = dto.EmployeeTypeId;
        entity.EffectiveStart = dto.EffectiveStart.Date;
        entity.EffectiveEnd = dto.EffectiveEnd?.Date;
        entity.FixedSalary = dto.FixedSalary;
        entity.HourlyRate = dto.HourlyRate;
        entity.PieceworkUnitPrice = dto.PieceworkUnitPrice;
        entity.BaseSalaryForPiecework = dto.BaseSalaryForPiecework;
        entity.MealSubsidyPerDay = dto.MealSubsidyPerDay;
        entity.NightSubsidyPerDay = dto.NightSubsidyPerDay;
        entity.PerformanceBase = dto.PerformanceBase;
        entity.DefaultOvertimeMultiplier = dto.DefaultOvertimeMultiplier;
        entity.IsActive = dto.IsActive;
        entity.Remark = dto.Remark;

        await _salaryRuleRepository.UpdateAsync(entity);
        entity.EmployeeType = employeeType;
        return MapToDto(entity);
    }

    private static SalaryRuleDto MapToDto(SalaryRule entity)
    {
        return new SalaryRuleDto
        {
            Id = entity.Id,
            RuleCode = entity.RuleCode,
            RuleName = entity.RuleName,
            EmployeeTypeId = entity.EmployeeTypeId,
            EmployeeTypeName = entity.EmployeeType?.TypeName ?? string.Empty,
            EffectiveStart = entity.EffectiveStart,
            EffectiveEnd = entity.EffectiveEnd,
            FixedSalary = entity.FixedSalary,
            HourlyRate = entity.HourlyRate,
            PieceworkUnitPrice = entity.PieceworkUnitPrice,
            BaseSalaryForPiecework = entity.BaseSalaryForPiecework,
            MealSubsidyPerDay = entity.MealSubsidyPerDay,
            NightSubsidyPerDay = entity.NightSubsidyPerDay,
            PerformanceBase = entity.PerformanceBase,
            DefaultOvertimeMultiplier = entity.DefaultOvertimeMultiplier,
            IsActive = entity.IsActive,
            Remark = entity.Remark
        };
    }
}
