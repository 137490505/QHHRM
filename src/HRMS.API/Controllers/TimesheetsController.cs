using HRMS.Application.DTOs;
using HRMS.Application.Services;
using HRMS.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.API.Controllers;

[ApiController]
[Route("api/v1/timesheets")]
public class TimesheetsController : ControllerBase
{
    private readonly TimesheetService _service;

    public TimesheetsController(TimesheetService service)
    {
        _service = service;
    }

    [HttpGet]
    [RequirePermission("page.timesheet.list")]
    public async Task<IActionResult> GetAll()
    {
        var result = await _service.GetAllAsync();
        return Ok(new { code = 200, message = "success", data = result });
    }

    [HttpGet("{id:guid}")]
    [RequirePermission("page.timesheet.list")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        return result == null
            ? NotFound(new { code = 404, message = "工时记录不存在" })
            : Ok(new { code = 200, message = "success", data = result });
    }

    [HttpGet("employee/{employeeId:guid}")]
    [RequirePermission("page.timesheet.list")]
    public async Task<IActionResult> GetByEmployee(Guid employeeId, [FromQuery] int year, [FromQuery] int month)
    {
        var result = await _service.GetByEmployeeAndMonthAsync(employeeId, year, month);
        return Ok(new { code = 200, message = "success", data = result });
    }

    [HttpGet("org-unit/{orgUnitId:guid}")]
    [RequirePermission("page.timesheet.list")]
    public async Task<IActionResult> GetByOrgUnit(Guid orgUnitId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        var result = await _service.GetByOrgUnitAndDateRangeAsync(orgUnitId, startDate, endDate);
        return Ok(new { code = 200, message = "success", data = result });
    }

    [HttpPost]
    [RequirePermission("button.timesheet.create")]
    public async Task<IActionResult> Create([FromBody] CreateTimesheetDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, new { code = 200, message = "success", data = result });
    }

    [HttpPost("import")]
    [RequirePermission("button.timesheet.import")]
    public async Task<IActionResult> Import([FromBody] List<ImportTimesheetDto> dtos)
    {
        var count = await _service.ImportBatchAsync(dtos);
        return Ok(new { code = 200, message = $"成功导入 {count} 条记录", data = new { count } });
    }

    [HttpPost("{id:guid}/approve")]
    [RequirePermission("button.timesheet.approve")]
    public async Task<IActionResult> Approve(Guid id, [FromQuery] Guid approverId)
    {
        var success = await _service.ApproveAsync(id, approverId);
        return success
            ? Ok(new { code = 200, message = "审批成功" })
            : NotFound(new { code = 404, message = "工时记录不存在" });
    }
}
