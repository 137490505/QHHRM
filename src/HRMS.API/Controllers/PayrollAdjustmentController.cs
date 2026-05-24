using HRMS.API.Services;
using HRMS.Application.DTOs;
using HRMS.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.API.Controllers;

[ApiController]
[Route("api/v1/payroll/adjustments")]
public class PayrollAdjustmentController : ControllerBase
{
    private readonly SalaryAdjustmentService _service;

    public PayrollAdjustmentController(SalaryAdjustmentService service)
    {
        _service = service;
    }

    [HttpGet]
    [RequirePermission("page.salary.list")]
    public async Task<IActionResult> GetByMonth([FromQuery] string yearMonth)
    {
        var result = await _service.GetByMonthAsync(yearMonth);
        return Ok(new { code = 200, message = "success", data = result });
    }

    [HttpGet("{id:guid}")]
    [RequirePermission("page.salary.list")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        return result == null
            ? NotFound(new { code = 404, message = "薪资调整项不存在" })
            : Ok(new { code = 200, message = "success", data = result });
    }

    [HttpGet("employee/{employeeId:guid}")]
    [RequirePermission("page.salary.list")]
    public async Task<IActionResult> GetByEmployee(Guid employeeId, [FromQuery] string yearMonth)
    {
        var result = await _service.GetByEmployeeAndMonthAsync(employeeId, yearMonth);
        return Ok(new { code = 200, message = "success", data = result });
    }

    [HttpPost]
    [RequirePermission("button.salary.adjust")]
    public async Task<IActionResult> Create([FromBody] CreateSalaryAdjustmentDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            return BadRequest(new { code = 400, message = "验证失败", errors });
        }

        var result = await _service.CreateAsync(dto);
        return Ok(new { code = 200, message = "薪资调整项创建成功", data = result });
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("button.salary.adjust")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSalaryAdjustmentDto dto)
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

        var result = await _service.UpdateAsync(dto);
        return result == null
            ? NotFound(new { code = 404, message = "薪资调整项不存在" })
            : Ok(new { code = 200, message = "薪资调整项更新成功", data = result });
    }
}
