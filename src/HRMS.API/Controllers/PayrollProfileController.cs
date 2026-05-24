using HRMS.API.Services;
using HRMS.Application.DTOs;
using HRMS.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.API.Controllers;

[ApiController]
[Route("api/v1/payroll/profiles")]
public class PayrollProfileController : ControllerBase
{
    private readonly EmployeePayrollProfileService _service;

    public PayrollProfileController(EmployeePayrollProfileService service)
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

    [HttpGet("{employeeId:guid}")]
    [RequirePermission("page.salary.list")]
    public async Task<IActionResult> GetByEmployee(Guid employeeId)
    {
        var result = await _service.GetByEmployeeIdAsync(employeeId);
        return result == null
            ? NotFound(new { code = 404, message = "员工薪资档案不存在" })
            : Ok(new { code = 200, message = "success", data = result });
    }

    [HttpPost]
    [RequirePermission("button.salary.adjust")]
    public async Task<IActionResult> Create([FromBody] CreateEmployeePayrollProfileDto dto)
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
        return Ok(new { code = 200, message = "员工薪资档案创建成功", data = result });
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("button.salary.adjust")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEmployeePayrollProfileDto dto)
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
            ? NotFound(new { code = 404, message = "员工薪资档案不存在" })
            : Ok(new { code = 200, message = "员工薪资档案更新成功", data = result });
    }
}
