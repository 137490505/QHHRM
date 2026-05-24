using HRMS.Application.DTOs;
using HRMS.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.API.Controllers;

[ApiController]
[Route("api/v1/third-party-bills")]
public class ThirdPartyBillsController : ControllerBase
{
    private readonly ThirdPartyBillService _service;

    public ThirdPartyBillsController(ThirdPartyBillService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _service.GetAllAsync();
        return Ok(new { code = 200, message = "success", data = result });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        return result == null
            ? NotFound(new { code = 404, message = "账单不存在" })
            : Ok(new { code = 200, message = "success", data = result });
    }

    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] GenerateBillDto dto)
    {
        var result = await _service.GenerateBillAsync(dto);
        return result == null
            ? NotFound(new { code = 404, message = "没有可核算的数据" })
            : Ok(new { code = 200, message = "账单生成成功", data = result });
    }
}