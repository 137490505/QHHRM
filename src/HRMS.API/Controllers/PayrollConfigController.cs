using HRMS.API.Services;
using HRMS.Application.DTOs;
using HRMS.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.API.Controllers;

[ApiController]
[Route("api/v1/payroll")]
public class PayrollConfigController : ControllerBase
{
    private readonly EmployeeTypeService _employeeTypeService;
    private readonly SalaryRuleService _salaryRuleService;
    private readonly OvertimeRateConfigService _overtimeRateConfigService;
    private readonly HolidayRuleService _holidayRuleService;
    private readonly IncomeTaxRuleService _incomeTaxRuleService;

    public PayrollConfigController(
        EmployeeTypeService employeeTypeService,
        SalaryRuleService salaryRuleService,
        OvertimeRateConfigService overtimeRateConfigService,
        HolidayRuleService holidayRuleService,
        IncomeTaxRuleService incomeTaxRuleService)
    {
        _employeeTypeService = employeeTypeService;
        _salaryRuleService = salaryRuleService;
        _overtimeRateConfigService = overtimeRateConfigService;
        _holidayRuleService = holidayRuleService;
        _incomeTaxRuleService = incomeTaxRuleService;
    }

    [HttpGet("employee-types")]
    [RequirePermission("page.salary.list")]
    public async Task<IActionResult> GetEmployeeTypes()
    {
        var result = await _employeeTypeService.GetAllAsync();
        return Ok(new { code = 200, message = "success", data = result });
    }

    [HttpGet("employee-types/{id:guid}")]
    [RequirePermission("page.salary.list")]
    public async Task<IActionResult> GetEmployeeType(Guid id)
    {
        var result = await _employeeTypeService.GetByIdAsync(id);
        return result == null
            ? NotFound(new { code = 404, message = "员工类型不存在" })
            : Ok(new { code = 200, message = "success", data = result });
    }

    [HttpPost("employee-types")]
    [RequirePermission("button.salary.adjust")]
    public async Task<IActionResult> CreateEmployeeType([FromBody] CreatePayrollEmployeeTypeDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            return BadRequest(new { code = 400, message = "验证失败", errors });
        }

        var result = await _employeeTypeService.CreateAsync(dto);
        return Ok(new { code = 200, message = "员工类型创建成功", data = result });
    }

    [HttpPut("employee-types/{id:guid}")]
    [RequirePermission("button.salary.adjust")]
    public async Task<IActionResult> UpdateEmployeeType(Guid id, [FromBody] UpdatePayrollEmployeeTypeDto dto)
    {
        if (id != dto.Id)
        {
            return BadRequest(new { code = 400, message = "路径参数与实体 ID 不一致" });
        }

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            return BadRequest(new { code = 400, message = "验证失败", errors });
        }

        var result = await _employeeTypeService.UpdateAsync(dto);
        return result == null
            ? NotFound(new { code = 404, message = "员工类型不存在" })
            : Ok(new { code = 200, message = "员工类型更新成功", data = result });
    }

    [HttpGet("salary-rules")]
    [RequirePermission("page.salary.list")]
    public async Task<IActionResult> GetSalaryRules([FromQuery] Guid? employeeTypeId)
    {
        var result = await _salaryRuleService.GetAllAsync(employeeTypeId);
        return Ok(new { code = 200, message = "success", data = result });
    }

    [HttpGet("salary-rules/{id:guid}")]
    [RequirePermission("page.salary.list")]
    public async Task<IActionResult> GetSalaryRule(Guid id)
    {
        var result = await _salaryRuleService.GetByIdAsync(id);
        return result == null
            ? NotFound(new { code = 404, message = "薪资规则不存在" })
            : Ok(new { code = 200, message = "success", data = result });
    }

    [HttpPost("salary-rules")]
    [RequirePermission("button.salary.adjust")]
    public async Task<IActionResult> CreateSalaryRule([FromBody] CreateSalaryRuleDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            return BadRequest(new { code = 400, message = "验证失败", errors });
        }

        var result = await _salaryRuleService.CreateAsync(dto);
        return Ok(new { code = 200, message = "薪资规则创建成功", data = result });
    }

    [HttpPut("salary-rules/{id:guid}")]
    [RequirePermission("button.salary.adjust")]
    public async Task<IActionResult> UpdateSalaryRule(Guid id, [FromBody] UpdateSalaryRuleDto dto)
    {
        if (id != dto.Id)
        {
            return BadRequest(new { code = 400, message = "路径参数与实体 ID 不一致" });
        }

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            return BadRequest(new { code = 400, message = "验证失败", errors });
        }

        var result = await _salaryRuleService.UpdateAsync(dto);
        return result == null
            ? NotFound(new { code = 404, message = "薪资规则不存在" })
            : Ok(new { code = 200, message = "薪资规则更新成功", data = result });
    }

    [HttpGet("overtime-rate-configs")]
    [RequirePermission("page.salary.list")]
    public async Task<IActionResult> GetOvertimeRateConfigs([FromQuery] Guid? employeeTypeId)
    {
        var result = await _overtimeRateConfigService.GetAllAsync(employeeTypeId);
        return Ok(new { code = 200, message = "success", data = result });
    }

    [HttpGet("overtime-rate-configs/{id:guid}")]
    [RequirePermission("page.salary.list")]
    public async Task<IActionResult> GetOvertimeRateConfig(Guid id)
    {
        var result = await _overtimeRateConfigService.GetByIdAsync(id);
        return result == null
            ? NotFound(new { code = 404, message = "加班费率配置不存在" })
            : Ok(new { code = 200, message = "success", data = result });
    }

    [HttpPost("overtime-rate-configs")]
    [RequirePermission("button.salary.adjust")]
    public async Task<IActionResult> CreateOvertimeRateConfig([FromBody] CreateOvertimeRateConfigDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            return BadRequest(new { code = 400, message = "验证失败", errors });
        }

        var result = await _overtimeRateConfigService.CreateAsync(dto);
        return Ok(new { code = 200, message = "加班费率配置创建成功", data = result });
    }

    [HttpPut("overtime-rate-configs/{id:guid}")]
    [RequirePermission("button.salary.adjust")]
    public async Task<IActionResult> UpdateOvertimeRateConfig(Guid id, [FromBody] UpdateOvertimeRateConfigDto dto)
    {
        if (id != dto.Id)
        {
            return BadRequest(new { code = 400, message = "路径参数与实体 ID 不一致" });
        }

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            return BadRequest(new { code = 400, message = "验证失败", errors });
        }

        var result = await _overtimeRateConfigService.UpdateAsync(dto);
        return result == null
            ? NotFound(new { code = 404, message = "加班费率配置不存在" })
            : Ok(new { code = 200, message = "加班费率配置更新成功", data = result });
    }

    [HttpGet("holiday-rules")]
    [RequirePermission("page.salary.list")]
    public async Task<IActionResult> GetHolidayRules([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        var result = await _holidayRuleService.GetAllAsync(startDate, endDate);
        return Ok(new { code = 200, message = "success", data = result });
    }

    [HttpGet("holiday-rules/{id:guid}")]
    [RequirePermission("page.salary.list")]
    public async Task<IActionResult> GetHolidayRule(Guid id)
    {
        var result = await _holidayRuleService.GetByIdAsync(id);
        return result == null
            ? NotFound(new { code = 404, message = "节假日规则不存在" })
            : Ok(new { code = 200, message = "success", data = result });
    }

    [HttpPost("holiday-rules")]
    [RequirePermission("button.salary.adjust")]
    public async Task<IActionResult> CreateHolidayRule([FromBody] CreateHolidayRuleDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            return BadRequest(new { code = 400, message = "验证失败", errors });
        }

        var result = await _holidayRuleService.CreateAsync(dto);
        return Ok(new { code = 200, message = "节假日规则创建成功", data = result });
    }

    [HttpPut("holiday-rules/{id:guid}")]
    [RequirePermission("button.salary.adjust")]
    public async Task<IActionResult> UpdateHolidayRule(Guid id, [FromBody] UpdateHolidayRuleDto dto)
    {
        if (id != dto.Id)
        {
            return BadRequest(new { code = 400, message = "路径参数与实体 ID 不一致" });
        }

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            return BadRequest(new { code = 400, message = "验证失败", errors });
        }

        var result = await _holidayRuleService.UpdateAsync(dto);
        return result == null
            ? NotFound(new { code = 404, message = "节假日规则不存在" })
            : Ok(new { code = 200, message = "节假日规则更新成功", data = result });
    }

    [HttpGet("income-tax-rules")]
    [RequirePermission("page.salary.list")]
    public async Task<IActionResult> GetIncomeTaxRules([FromQuery] int? ruleYear)
    {
        var result = await _incomeTaxRuleService.GetAllAsync(ruleYear);
        return Ok(new { code = 200, message = "success", data = result });
    }

    [HttpGet("income-tax-rules/{id:guid}")]
    [RequirePermission("page.salary.list")]
    public async Task<IActionResult> GetIncomeTaxRule(Guid id)
    {
        var result = await _incomeTaxRuleService.GetByIdAsync(id);
        return result == null
            ? NotFound(new { code = 404, message = "个税规则不存在" })
            : Ok(new { code = 200, message = "success", data = result });
    }

    [HttpPost("income-tax-rules")]
    [RequirePermission("button.salary.adjust")]
    public async Task<IActionResult> CreateIncomeTaxRule([FromBody] CreateIncomeTaxRuleDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            return BadRequest(new { code = 400, message = "验证失败", errors });
        }

        var result = await _incomeTaxRuleService.CreateAsync(dto);
        return Ok(new { code = 200, message = "个税规则创建成功", data = result });
    }

    [HttpPut("income-tax-rules/{id:guid}")]
    [RequirePermission("button.salary.adjust")]
    public async Task<IActionResult> UpdateIncomeTaxRule(Guid id, [FromBody] UpdateIncomeTaxRuleDto dto)
    {
        if (id != dto.Id)
        {
            return BadRequest(new { code = 400, message = "路径参数与实体 ID 不一致" });
        }

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            return BadRequest(new { code = 400, message = "验证失败", errors });
        }

        var result = await _incomeTaxRuleService.UpdateAsync(dto);
        return result == null
            ? NotFound(new { code = 404, message = "个税规则不存在" })
            : Ok(new { code = 200, message = "个税规则更新成功", data = result });
    }
}
