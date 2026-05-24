using HRMS.API.Services;
using HRMS.Application.DTOs;
using HRMS.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.API.Controllers;

[ApiController]
[Route("api/v1/customers")]
public class CustomersController : ControllerBase
{
    private readonly CustomerService _service;

    public CustomersController(CustomerService service)
    {
        _service = service;
    }

    [HttpGet]
    [RequirePermission("page.partner.customer")]
    public async Task<IActionResult> GetAll()
    {
        var result = await _service.GetAllAsync();
        return Ok(new { code = 200, message = "success", data = result });
    }

    [HttpGet("{id:guid}")]
    [RequirePermission("page.partner.customer")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        return result == null
            ? NotFound(new { code = 404, message = "客户不存在" })
            : Ok(new { code = 200, message = "success", data = result });
    }

    [HttpPost]
    [RequirePermission("button.partner.customer.create")]
    public async Task<IActionResult> Create([FromBody] CreateCustomerDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage).ToList();
            return BadRequest(new { code = 400, message = "验证失败", errors });
        }

        var result = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, new { code = 200, message = "success", data = result });
    }

    [HttpPut]
    [RequirePermission("button.partner.customer.edit")]
    public async Task<IActionResult> Update([FromBody] UpdateCustomerDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage).ToList();
            return BadRequest(new { code = 400, message = "验证失败", errors });
        }

        var result = await _service.UpdateAsync(dto);
        return result == null
            ? NotFound(new { code = 404, message = "客户不存在" })
            : Ok(new { code = 200, message = "success", data = result });
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission("button.partner.customer.delete")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var success = await _service.DeleteAsync(id);
        return success
            ? Ok(new { code = 200, message = "删除成功" })
            : NotFound(new { code = 404, message = "客户不存在" });
    }
}
