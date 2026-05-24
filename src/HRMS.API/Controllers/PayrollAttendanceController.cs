using HRMS.API.Services;
using HRMS.Application.DTOs;
using HRMS.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.API.Controllers;

[ApiController]
[Route("api/v1/payroll/attendance")]
public class PayrollAttendanceController : ControllerBase
{
    private readonly AttendanceService _service;

    public PayrollAttendanceController(AttendanceService service)
    {
        _service = service;
    }

    [HttpGet]
    [RequirePermission("page.salary.list")]
    public async Task<IActionResult> GetByMonth([FromQuery] int year, [FromQuery] int month)
    {
        var result = await _service.GetByMonthAsync(year, month);
        return Ok(new { code = 200, message = "success", data = result });
    }

    [HttpGet("{id:guid}")]
    [RequirePermission("page.salary.list")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        return result == null
            ? NotFound(new { code = 404, message = "考勤记录不存在" })
            : Ok(new { code = 200, message = "success", data = result });
    }

    [HttpGet("employee/{employeeId:guid}")]
    [RequirePermission("page.salary.list")]
    public async Task<IActionResult> GetByEmployee(Guid employeeId, [FromQuery] int year, [FromQuery] int month)
    {
        var result = await _service.GetByEmployeeAndMonthAsync(employeeId, year, month);
        return Ok(new { code = 200, message = "success", data = result });
    }

    [HttpPost]
    [RequirePermission("button.salary.adjust")]
    public async Task<IActionResult> Create([FromBody] CreateAttendanceDto dto)
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
        return Ok(new { code = 200, message = "考勤记录创建成功", data = result });
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("button.salary.adjust")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAttendanceDto dto)
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
            ? NotFound(new { code = 404, message = "考勤记录不存在" })
            : Ok(new { code = 200, message = "考勤记录更新成功", data = result });
    }
}
