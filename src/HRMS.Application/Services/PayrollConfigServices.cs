using HRMS.Application.DTOs;
using HRMS.Domain.Entities;
using HRMS.Domain.Interfaces;

namespace HRMS.Application.Services;

public class OvertimeRateConfigService
{
    private readonly IOvertimeRateConfigRepository _overtimeRateConfigRepository;
    private readonly IEmployeeTypeRepository _employeeTypeRepository;

    public OvertimeRateConfigService(
        IOvertimeRateConfigRepository overtimeRateConfigRepository,
        IEmployeeTypeRepository employeeTypeRepository)
    {
        _overtimeRateConfigRepository = overtimeRateConfigRepository;
        _employeeTypeRepository = employeeTypeRepository;
    }

    public async Task<IEnumerable<OvertimeRateConfigDto>> GetAllAsync(Guid? employeeTypeId = null)
    {
        var entities = employeeTypeId.HasValue
            ? await _overtimeRateConfigRepository.GetByEmployeeTypeAsync(employeeTypeId.Value)
            : await _overtimeRateConfigRepository.GetAllAsync();

        return entities.Select(MapToDto);
    }

    public async Task<OvertimeRateConfigDto?> GetByIdAsync(Guid id)
    {
        var entity = await _overtimeRateConfigRepository.GetByIdAsync(id);
        return entity == null ? null : MapToDto(entity);
    }

    public async Task<OvertimeRateConfigDto> CreateAsync(CreateOvertimeRateConfigDto dto)
    {
        var configCode = NormalizeRequiredText(dto.ConfigCode, "配置编码不能为空");
        var holidayType = NormalizeRequiredText(dto.HolidayType, "加班类型不能为空");
        ValidateEffectiveRange(dto.EffectiveStart, dto.EffectiveEnd);

        var employeeType = await _employeeTypeRepository.GetByIdAsync(dto.EmployeeTypeId)
            ?? throw new InvalidOperationException("员工类型不存在");

        var existing = await _overtimeRateConfigRepository.GetByCodeAsync(configCode);
        if (existing != null)
        {
            throw new InvalidOperationException("加班费率配置编码已存在");
        }

        var entity = new OvertimeRateConfig
        {
            ConfigCode = configCode,
            EmployeeTypeId = dto.EmployeeTypeId,
            HolidayType = holidayType,
            Multiplier = dto.Multiplier,
            EffectiveStart = dto.EffectiveStart.Date,
            EffectiveEnd = dto.EffectiveEnd?.Date,
            IsActive = dto.IsActive,
            Remark = dto.Remark
        };

        var created = await _overtimeRateConfigRepository.CreateAsync(entity);
        created.EmployeeType = employeeType;
        return MapToDto(created);
    }

    public async Task<OvertimeRateConfigDto?> UpdateAsync(UpdateOvertimeRateConfigDto dto)
    {
        var entity = await _overtimeRateConfigRepository.GetByIdAsync(dto.Id);
        if (entity == null)
        {
            return null;
        }

        var configCode = NormalizeRequiredText(dto.ConfigCode, "配置编码不能为空");
        var holidayType = NormalizeRequiredText(dto.HolidayType, "加班类型不能为空");
        ValidateEffectiveRange(dto.EffectiveStart, dto.EffectiveEnd);

        var employeeType = await _employeeTypeRepository.GetByIdAsync(dto.EmployeeTypeId)
            ?? throw new InvalidOperationException("员工类型不存在");

        if (!string.Equals(entity.ConfigCode, configCode, StringComparison.OrdinalIgnoreCase))
        {
            var existing = await _overtimeRateConfigRepository.GetByCodeAsync(configCode);
            if (existing != null && existing.Id != dto.Id)
            {
                throw new InvalidOperationException("加班费率配置编码已存在");
            }
        }

        entity.ConfigCode = configCode;
        entity.EmployeeTypeId = dto.EmployeeTypeId;
        entity.HolidayType = holidayType;
        entity.Multiplier = dto.Multiplier;
        entity.EffectiveStart = dto.EffectiveStart.Date;
        entity.EffectiveEnd = dto.EffectiveEnd?.Date;
        entity.IsActive = dto.IsActive;
        entity.Remark = dto.Remark;

        await _overtimeRateConfigRepository.UpdateAsync(entity);
        entity.EmployeeType = employeeType;
        return MapToDto(entity);
    }

    private static OvertimeRateConfigDto MapToDto(OvertimeRateConfig entity)
    {
        return new OvertimeRateConfigDto
        {
            Id = entity.Id,
            ConfigCode = entity.ConfigCode,
            EmployeeTypeId = entity.EmployeeTypeId,
            EmployeeTypeName = entity.EmployeeType?.TypeName ?? string.Empty,
            HolidayType = entity.HolidayType,
            Multiplier = entity.Multiplier,
            EffectiveStart = entity.EffectiveStart,
            EffectiveEnd = entity.EffectiveEnd,
            IsActive = entity.IsActive,
            Remark = entity.Remark
        };
    }

    private static string NormalizeRequiredText(string? value, string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(errorMessage);
        }

        return value.Trim();
    }

    private static void ValidateEffectiveRange(DateTime effectiveStart, DateTime? effectiveEnd)
    {
        if (effectiveEnd.HasValue && effectiveEnd.Value.Date < effectiveStart.Date)
        {
            throw new InvalidOperationException("生效结束时间不能早于生效开始时间");
        }
    }
}

public class HolidayRuleService
{
    private readonly IHolidayRuleRepository _holidayRuleRepository;

    public HolidayRuleService(IHolidayRuleRepository holidayRuleRepository)
    {
        _holidayRuleRepository = holidayRuleRepository;
    }

    public async Task<IEnumerable<HolidayRuleDto>> GetAllAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        if (startDate.HasValue && endDate.HasValue && endDate.Value.Date < startDate.Value.Date)
        {
            throw new InvalidOperationException("结束日期不能早于开始日期");
        }

        IEnumerable<HolidayRule> entities;
        if (startDate.HasValue && endDate.HasValue)
        {
            entities = await _holidayRuleRepository.GetByDateRangeAsync(startDate.Value.Date, endDate.Value.Date);
        }
        else
        {
            entities = await _holidayRuleRepository.GetAllAsync();
        }

        return entities.Select(MapToDto);
    }

    public async Task<HolidayRuleDto?> GetByIdAsync(Guid id)
    {
        var entity = await _holidayRuleRepository.GetByIdAsync(id);
        return entity == null ? null : MapToDto(entity);
    }

    public async Task<HolidayRuleDto> CreateAsync(CreateHolidayRuleDto dto)
    {
        var holidayCode = NormalizeRequiredText(dto.HolidayCode, "节假日编码不能为空");
        var holidayName = NormalizeRequiredText(dto.HolidayName, "节假日名称不能为空");
        var holidayType = NormalizeRequiredText(dto.HolidayType, "节假日类型不能为空");

        var existingCode = await _holidayRuleRepository.GetByCodeAsync(holidayCode);
        if (existingCode != null)
        {
            throw new InvalidOperationException("节假日编码已存在");
        }

        var existingDateType = await _holidayRuleRepository.GetByDateAndTypeAsync(dto.HolidayDate.Date, holidayType);
        if (existingDateType != null)
        {
            throw new InvalidOperationException("同一天的节假日类型配置已存在");
        }

        var entity = new HolidayRule
        {
            HolidayCode = holidayCode,
            HolidayName = holidayName,
            HolidayDate = dto.HolidayDate.Date,
            HolidayType = holidayType,
            OvertimeMultiplier = dto.OvertimeMultiplier,
            IsActive = dto.IsActive,
            Remark = dto.Remark
        };

        var created = await _holidayRuleRepository.CreateAsync(entity);
        return MapToDto(created);
    }

    public async Task<HolidayRuleDto?> UpdateAsync(UpdateHolidayRuleDto dto)
    {
        var entity = await _holidayRuleRepository.GetByIdAsync(dto.Id);
        if (entity == null)
        {
            return null;
        }

        var holidayCode = NormalizeRequiredText(dto.HolidayCode, "节假日编码不能为空");
        var holidayName = NormalizeRequiredText(dto.HolidayName, "节假日名称不能为空");
        var holidayType = NormalizeRequiredText(dto.HolidayType, "节假日类型不能为空");

        if (!string.Equals(entity.HolidayCode, holidayCode, StringComparison.OrdinalIgnoreCase))
        {
            var existingCode = await _holidayRuleRepository.GetByCodeAsync(holidayCode);
            if (existingCode != null && existingCode.Id != dto.Id)
            {
                throw new InvalidOperationException("节假日编码已存在");
            }
        }

        var existingDateType = await _holidayRuleRepository.GetByDateAndTypeAsync(dto.HolidayDate.Date, holidayType);
        if (existingDateType != null && existingDateType.Id != dto.Id)
        {
            throw new InvalidOperationException("同一天的节假日类型配置已存在");
        }

        entity.HolidayCode = holidayCode;
        entity.HolidayName = holidayName;
        entity.HolidayDate = dto.HolidayDate.Date;
        entity.HolidayType = holidayType;
        entity.OvertimeMultiplier = dto.OvertimeMultiplier;
        entity.IsActive = dto.IsActive;
        entity.Remark = dto.Remark;

        await _holidayRuleRepository.UpdateAsync(entity);
        return MapToDto(entity);
    }

    private static HolidayRuleDto MapToDto(HolidayRule entity)
    {
        return new HolidayRuleDto
        {
            Id = entity.Id,
            HolidayCode = entity.HolidayCode,
            HolidayName = entity.HolidayName,
            HolidayDate = entity.HolidayDate,
            HolidayType = entity.HolidayType,
            OvertimeMultiplier = entity.OvertimeMultiplier,
            IsActive = entity.IsActive,
            Remark = entity.Remark
        };
    }

    private static string NormalizeRequiredText(string? value, string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(errorMessage);
        }

        return value.Trim();
    }
}

public class IncomeTaxRuleService
{
    private readonly IIncomeTaxRuleRepository _incomeTaxRuleRepository;

    public IncomeTaxRuleService(IIncomeTaxRuleRepository incomeTaxRuleRepository)
    {
        _incomeTaxRuleRepository = incomeTaxRuleRepository;
    }

    public async Task<IEnumerable<IncomeTaxRuleDto>> GetAllAsync(int? ruleYear = null)
    {
        var entities = ruleYear.HasValue
            ? await _incomeTaxRuleRepository.GetByYearAsync(ruleYear.Value)
            : await _incomeTaxRuleRepository.GetAllAsync();

        return entities.Select(MapToDto);
    }

    public async Task<IncomeTaxRuleDto?> GetByIdAsync(Guid id)
    {
        var entity = await _incomeTaxRuleRepository.GetByIdAsync(id);
        return entity == null ? null : MapToDto(entity);
    }

    public async Task<IncomeTaxRuleDto> CreateAsync(CreateIncomeTaxRuleDto dto)
    {
        ValidateRuleRange(dto.MinTaxableAmount, dto.MaxTaxableAmount, dto.TaxRate);

        var existing = await _incomeTaxRuleRepository.GetByYearAndLevelAsync(dto.RuleYear, dto.LevelNo);
        if (existing != null)
        {
            throw new InvalidOperationException("同一年份下的个税级次已存在");
        }

        var entity = new IncomeTaxRule
        {
            RuleYear = dto.RuleYear,
            LevelNo = dto.LevelNo,
            MinTaxableAmount = dto.MinTaxableAmount,
            MaxTaxableAmount = dto.MaxTaxableAmount,
            TaxRate = dto.TaxRate,
            QuickDeduction = dto.QuickDeduction,
            ThresholdAmount = dto.ThresholdAmount,
            IsActive = dto.IsActive,
            Remark = dto.Remark
        };

        var created = await _incomeTaxRuleRepository.CreateAsync(entity);
        return MapToDto(created);
    }

    public async Task<IncomeTaxRuleDto?> UpdateAsync(UpdateIncomeTaxRuleDto dto)
    {
        var entity = await _incomeTaxRuleRepository.GetByIdAsync(dto.Id);
        if (entity == null)
        {
            return null;
        }

        ValidateRuleRange(dto.MinTaxableAmount, dto.MaxTaxableAmount, dto.TaxRate);

        var existing = await _incomeTaxRuleRepository.GetByYearAndLevelAsync(dto.RuleYear, dto.LevelNo);
        if (existing != null && existing.Id != dto.Id)
        {
            throw new InvalidOperationException("同一年份下的个税级次已存在");
        }

        entity.RuleYear = dto.RuleYear;
        entity.LevelNo = dto.LevelNo;
        entity.MinTaxableAmount = dto.MinTaxableAmount;
        entity.MaxTaxableAmount = dto.MaxTaxableAmount;
        entity.TaxRate = dto.TaxRate;
        entity.QuickDeduction = dto.QuickDeduction;
        entity.ThresholdAmount = dto.ThresholdAmount;
        entity.IsActive = dto.IsActive;
        entity.Remark = dto.Remark;

        await _incomeTaxRuleRepository.UpdateAsync(entity);
        return MapToDto(entity);
    }

    private static IncomeTaxRuleDto MapToDto(IncomeTaxRule entity)
    {
        return new IncomeTaxRuleDto
        {
            Id = entity.Id,
            RuleYear = entity.RuleYear,
            LevelNo = entity.LevelNo,
            MinTaxableAmount = entity.MinTaxableAmount,
            MaxTaxableAmount = entity.MaxTaxableAmount,
            TaxRate = entity.TaxRate,
            QuickDeduction = entity.QuickDeduction,
            ThresholdAmount = entity.ThresholdAmount,
            IsActive = entity.IsActive,
            Remark = entity.Remark
        };
    }

    private static void ValidateRuleRange(decimal minTaxableAmount, decimal? maxTaxableAmount, decimal taxRate)
    {
        if (maxTaxableAmount.HasValue && maxTaxableAmount.Value < minTaxableAmount)
        {
            throw new InvalidOperationException("应税金额上限不能小于下限");
        }

        if (taxRate < 0 || taxRate > 1)
        {
            throw new InvalidOperationException("税率必须介于 0 和 1 之间");
        }
    }
}
