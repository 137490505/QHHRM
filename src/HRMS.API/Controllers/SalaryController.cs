using HRMS.Application.DTOs;
using HRMS.Application.Services;
using HRMS.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.API.Controllers;

[ApiController]
[Route("api/v1/salary")]
public class SalaryController : ControllerBase
{
    private readonly SalaryCalculationService _service;

    public SalaryController(SalaryCalculationService service)
    {
        _service = service;
    }

    [HttpGet]
    [RequirePermission("page.salary.list")]
    public async Task<IActionResult> GetAll()
    {
        var result = await _service.GetAllAsync();
        return Ok(new { code = 200, message = "success", data = result });
    }

    [HttpGet("{id:guid}")]
    [RequirePermission("page.salary.list")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        return result == null
            ? NotFound(new { code = 404, message = "薪资记录不存在" })
            : Ok(new { code = 200, message = "success", data = result });
    }

    [HttpGet("employee/{employeeId:guid}")]
    [RequirePermission("page.salary.list")]
    public async Task<IActionResult> GetByEmployee(Guid employeeId, [FromQuery] int year, [FromQuery] int month)
    {
        var result = await _service.GetByEmployeeAndMonthAsync(employeeId, year, month);
        return result == null
            ? NotFound(new { code = 404, message = "薪资记录不存在" })
            : Ok(new { code = 200, message = "success", data = result });
    }

    [HttpPost("calculate")]
    [RequirePermission("button.salary.calculate")]
    public async Task<IActionResult> Calculate([FromBody] CalculateSalaryDto dto)
    {
        var result = await _service.CalculateAsync(dto);
        return Ok(new { code = 200, message = "薪资核算完成", data = result });
    }
}
