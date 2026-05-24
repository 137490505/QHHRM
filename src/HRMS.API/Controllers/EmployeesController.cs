using HRMS.Application.DTOs;
using HRMS.Application.Services;
using HRMS.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.API.Controllers;

[ApiController]
[Route("api/v1/employees")]
public class EmployeesController : ControllerBase
{
    private readonly EmployeeService _service;

    public EmployeesController(EmployeeService service)
    {
        _service = service;
    }

    [HttpGet]
    [RequirePermission("page.employee.list")]
    public async Task<IActionResult> GetAll()
    {
        var result = await _service.GetAllAsync();
        return Ok(new { code = 200, message = "success", data = result });
    }

    [HttpGet("{id:guid}")]
    [RequirePermission("page.employee.list")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        return result == null
            ? NotFound(new { code = 404, message = "员工不存在" })
            : Ok(new { code = 200, message = "success", data = result });
    }

    [HttpGet("no/{employeeNo}")]
    [RequirePermission("page.employee.list")]
    public async Task<IActionResult> GetByEmployeeNo(string employeeNo)
    {
        var result = await _service.GetByEmployeeNoAsync(employeeNo);
        return result == null
            ? NotFound(new { code = 404, message = "员工不存在" })
            : Ok(new { code = 200, message = "success", data = result });
    }

    [HttpGet("org-unit/{orgUnitId:guid}")]
    [RequirePermission("page.employee.list")]
    public async Task<IActionResult> GetByOrgUnit(Guid orgUnitId)
    {
        var result = await _service.GetByOrgUnitIdAsync(orgUnitId);
        return Ok(new { code = 200, message = "success", data = result });
    }

    [HttpGet("third-party/{companyId:guid}")]
    [RequirePermission("page.employee.list")]
    public async Task<IActionResult> GetByThirdPartyCompany(Guid companyId)
    {
        var result = await _service.GetByThirdPartyCompanyAsync(companyId);
        return Ok(new { code = 200, message = "success", data = result });
    }

    [HttpPost]
    [RequirePermission("button.employee.create")]
    public async Task<IActionResult> Create([FromBody] CreateEmployeeDto dto)
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
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, new { code = 200, message = "success", data = result });
    }

    [HttpPut]
    [RequirePermission("button.employee.edit")]
    public async Task<IActionResult> Update([FromBody] UpdateEmployeeDto dto)
    {
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
            ? NotFound(new { code = 404, message = "员工不存在" })
            : Ok(new { code = 200, message = "success", data = result });
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission("button.employee.delete")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var success = await _service.DeleteAsync(id);
        return success
            ? Ok(new { code = 200, message = "删除成功" })
            : NotFound(new { code = 404, message = "员工不存在" });
    }

    [HttpPost("{id:guid}/toggle-dismiss")]
    [RequirePermission("button.employee.dismiss")]
    public async Task<IActionResult> ToggleDismiss(Guid id)
    {
        var success = await _service.ToggleDismissAsync(id);
        return success
            ? Ok(new { code = 200, message = "操作成功" })
            : NotFound(new { code = 404, message = "员工不存在" });
    }

    [HttpPost("batch-dismiss")]
    [RequirePermission("button.employee.dismiss")]
    public async Task<IActionResult> BatchDismiss([FromBody] BatchDismissDto dto)
    {
        await _service.BatchDismissAsync(dto);
        return Ok(new { code = 200, message = "批量办理离职成功" });
    }
}
