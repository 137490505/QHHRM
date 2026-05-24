using System.ComponentModel.DataAnnotations;

namespace HRMS.Application.DTOs;

public class PayrollEmployeeTypeDto
{
    public Guid Id { get; set; }
    public string TypeCode { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;
    public string SalaryMode { get; set; } = string.Empty;
    public bool HasOvertime { get; set; }
    public bool HasMealSubsidy { get; set; }
    public bool HasNightSubsidy { get; set; }
    public bool HasPerformance { get; set; }
    public bool HasSocialSecurity { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
    public string? Remark { get; set; }
}

public class CreatePayrollEmployeeTypeDto
{
    [Required(ErrorMessage = "类型编码不能为空")]
    public string TypeCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "类型名称不能为空")]
    public string TypeName { get; set; } = string.Empty;

    [Required(ErrorMessage = "薪资模式不能为空")]
    public string SalaryMode { get; set; } = string.Empty;

    public bool HasOvertime { get; set; }
    public bool HasMealSubsidy { get; set; }
    public bool HasNightSubsidy { get; set; }
    public bool HasPerformance { get; set; }
    public bool HasSocialSecurity { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public string? Remark { get; set; }
}

public class UpdatePayrollEmployeeTypeDto : CreatePayrollEmployeeTypeDto
{
    public Guid Id { get; set; }
}

public class SalaryRuleDto
{
    public Guid Id { get; set; }
    public string RuleCode { get; set; } = string.Empty;
    public string RuleName { get; set; } = string.Empty;
    public Guid EmployeeTypeId { get; set; }
    public string EmployeeTypeName { get; set; } = string.Empty;
    public DateTime EffectiveStart { get; set; }
    public DateTime? EffectiveEnd { get; set; }
    public decimal? FixedSalary { get; set; }
    public decimal? HourlyRate { get; set; }
    public decimal? PieceworkUnitPrice { get; set; }
    public decimal? BaseSalaryForPiecework { get; set; }
    public decimal MealSubsidyPerDay { get; set; }
    public decimal NightSubsidyPerDay { get; set; }
    public decimal? PerformanceBase { get; set; }
    public decimal DefaultOvertimeMultiplier { get; set; }
    public bool IsActive { get; set; }
    public string? Remark { get; set; }
}

public class CreateSalaryRuleDto
{
    [Required(ErrorMessage = "规则编码不能为空")]
    public string RuleCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "规则名称不能为空")]
    public string RuleName { get; set; } = string.Empty;

    [Required(ErrorMessage = "员工类型不能为空")]
    public Guid EmployeeTypeId { get; set; }

    [Required(ErrorMessage = "生效开始时间不能为空")]
    public DateTime EffectiveStart { get; set; }

    public DateTime? EffectiveEnd { get; set; }
    public decimal? FixedSalary { get; set; }
    public decimal? HourlyRate { get; set; }
    public decimal? PieceworkUnitPrice { get; set; }
    public decimal? BaseSalaryForPiecework { get; set; }
    public decimal MealSubsidyPerDay { get; set; }
    public decimal NightSubsidyPerDay { get; set; }
    public decimal? PerformanceBase { get; set; }
    public decimal DefaultOvertimeMultiplier { get; set; } = 1.5m;
    public bool IsActive { get; set; } = true;
    public string? Remark { get; set; }
}

public class UpdateSalaryRuleDto : CreateSalaryRuleDto
{
    public Guid Id { get; set; }
}

public class OvertimeRateConfigDto
{
    public Guid Id { get; set; }
    public string ConfigCode { get; set; } = string.Empty;
    public Guid EmployeeTypeId { get; set; }
    public string EmployeeTypeName { get; set; } = string.Empty;
    public string HolidayType { get; set; } = string.Empty;
    public decimal Multiplier { get; set; }
    public DateTime EffectiveStart { get; set; }
    public DateTime? EffectiveEnd { get; set; }
    public bool IsActive { get; set; }
    public string? Remark { get; set; }
}

public class CreateOvertimeRateConfigDto
{
    [Required(ErrorMessage = "配置编码不能为空")]
    public string ConfigCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "员工类型不能为空")]
    public Guid EmployeeTypeId { get; set; }

    [Required(ErrorMessage = "加班类型不能为空")]
    public string HolidayType { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "999.99", ErrorMessage = "加班倍数必须大于 0")]
    public decimal Multiplier { get; set; }

    [Required(ErrorMessage = "生效开始时间不能为空")]
    public DateTime EffectiveStart { get; set; }

    public DateTime? EffectiveEnd { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Remark { get; set; }
}

public class UpdateOvertimeRateConfigDto : CreateOvertimeRateConfigDto
{
    public Guid Id { get; set; }
}

public class HolidayRuleDto
{
    public Guid Id { get; set; }
    public string HolidayCode { get; set; } = string.Empty;
    public string HolidayName { get; set; } = string.Empty;
    public DateTime HolidayDate { get; set; }
    public string HolidayType { get; set; } = string.Empty;
    public decimal? OvertimeMultiplier { get; set; }
    public bool IsActive { get; set; }
    public string? Remark { get; set; }
}

public class CreateHolidayRuleDto
{
    [Required(ErrorMessage = "节假日编码不能为空")]
    public string HolidayCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "节假日名称不能为空")]
    public string HolidayName { get; set; } = string.Empty;

    [Required(ErrorMessage = "节假日日期不能为空")]
    public DateTime HolidayDate { get; set; }

    [Required(ErrorMessage = "节假日类型不能为空")]
    public string HolidayType { get; set; } = string.Empty;

    [Range(typeof(decimal), "0", "999.99", ErrorMessage = "节假日覆盖倍数不能小于 0")]
    public decimal? OvertimeMultiplier { get; set; }

    public bool IsActive { get; set; } = true;
    public string? Remark { get; set; }
}

public class UpdateHolidayRuleDto : CreateHolidayRuleDto
{
    public Guid Id { get; set; }
}

public class IncomeTaxRuleDto
{
    public Guid Id { get; set; }
    public int RuleYear { get; set; }
    public int LevelNo { get; set; }
    public decimal MinTaxableAmount { get; set; }
    public decimal? MaxTaxableAmount { get; set; }
    public decimal TaxRate { get; set; }
    public decimal QuickDeduction { get; set; }
    public decimal ThresholdAmount { get; set; }
    public bool IsActive { get; set; }
    public string? Remark { get; set; }
}

public class CreateIncomeTaxRuleDto
{
    [Range(2000, 2100, ErrorMessage = "规则年份不合法")]
    public int RuleYear { get; set; }

    [Range(1, 99, ErrorMessage = "税率级次必须大于 0")]
    public int LevelNo { get; set; }

    [Range(typeof(decimal), "0", "999999999", ErrorMessage = "应税金额下限不能小于 0")]
    public decimal MinTaxableAmount { get; set; }

    [Range(typeof(decimal), "0", "999999999", ErrorMessage = "应税金额上限不能小于 0")]
    public decimal? MaxTaxableAmount { get; set; }

    [Range(typeof(decimal), "0", "1", ErrorMessage = "税率必须介于 0 和 1 之间")]
    public decimal TaxRate { get; set; }

    [Range(typeof(decimal), "0", "999999999", ErrorMessage = "速算扣除数不能小于 0")]
    public decimal QuickDeduction { get; set; }

    [Range(typeof(decimal), "0", "999999999", ErrorMessage = "起征点不能小于 0")]
    public decimal ThresholdAmount { get; set; } = 5000m;

    public bool IsActive { get; set; } = true;
    public string? Remark { get; set; }
}

public class UpdateIncomeTaxRuleDto : CreateIncomeTaxRuleDto
{
    public Guid Id { get; set; }
}
