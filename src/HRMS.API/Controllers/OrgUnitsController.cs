using HRMS.Application.DTOs;
using HRMS.Application.Services;
using HRMS.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace HRMS.API.Controllers;

[ApiController]
[Route("api/v1/org-units")]
public class OrgUnitsController : ControllerBase
{
    private readonly OrgUnitService _service;
    private readonly ILogger<OrgUnitsController> _logger;

    public OrgUnitsController(OrgUnitService service, ILogger<OrgUnitsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    [RequirePermission("page.org.list")]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            _logger.LogInformation("Getting all org units");
            var result = await _service.GetAllAsync();
            _logger.LogInformation("Successfully retrieved {Count} org units", result.Count());
            return Ok(new { code = 200, message = "success", data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting org units");
            return StatusCode(500, new { code = 500, message = "服务器内部错误: " + ex.Message });
        }
    }

    [HttpGet("{id:guid}")]
    [RequirePermission("page.org.list")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        return result == null
            ? NotFound(new { code = 404, message = "组织单元不存在" })
            : Ok(new { code = 200, message = "success", data = result });
    }

    [HttpGet("code/{code}")]
    [RequirePermission("page.org.list")]
    public async Task<IActionResult> GetByCode(string code)
    {
        var result = await _service.GetByCodeAsync(code);
        return result == null
            ? NotFound(new { code = 404, message = "组织单元不存在" })
            : Ok(new { code = 200, message = "success", data = result });
    }

    [HttpGet("tree")]
    [RequirePermission("page.org.list")]
    public async Task<IActionResult> GetTree()
    {
        var result = await _service.GetTreeAsync();
        return Ok(new { code = 200, message = "success", data = result });
    }

    [HttpGet("{parentId:guid}/children")]
    [RequirePermission("page.org.list")]
    public async Task<IActionResult> GetChildren(Guid parentId)
    {
        var result = await _service.GetChildrenAsync(parentId);
        return Ok(new { code = 200, message = "success", data = result });
    }

    [HttpPost]
    [RequirePermission("button.org.create")]
    public async Task<IActionResult> Create([FromBody] CreateOrgUnitDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, new { code = 200, message = "success", data = result });
    }

    [HttpPut]
    [RequirePermission("button.org.edit")]
    public async Task<IActionResult> Update([FromBody] UpdateOrgUnitDto dto)
    {
        var result = await _service.UpdateAsync(dto);
        return result == null
            ? NotFound(new { code = 404, message = "组织单元不存在" })
            : Ok(new { code = 200, message = "success", data = result });
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission("button.org.delete")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var success = await _service.DeleteAsync(id);
            return success
                ? Ok(new { code = 200, message = "删除成功" })
                : NotFound(new { code = 404, message = "组织单元不存在" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { code = 400, message = ex.Message });
        }
    }

    [HttpPost("batch-enable")]
    [RequirePermission("button.org.enable")]
    public async Task<IActionResult> BatchEnable([FromBody] IEnumerable<Guid> ids)
    {
        await _service.BatchEnableAsync(ids);
        return Ok(new { code = 200, message = "批量启用成功" });
    }

    [HttpPost("batch-disable")]
    [RequirePermission("button.org.disable")]
    public async Task<IActionResult> BatchDisable([FromBody] IEnumerable<Guid> ids)
    {
        await _service.BatchDisableAsync(ids);
        return Ok(new { code = 200, message = "批量停用成功" });
    }

    [HttpPost("batch-update")]
    [RequirePermission("button.org.edit")]
    public async Task<IActionResult> BatchUpdate([FromBody] BatchUpdateOrgUnitDto dto)
    {
        try
        {
            await _service.BatchUpdateAsync(dto);
            return Ok(new { code = 200, message = "批量修改成功" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { code = 400, message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/toggle-status")]
    [RequirePermission("button.org.edit")]
    public async Task<IActionResult> ToggleStatus(Guid id)
    {
        var success = await _service.ToggleStatusAsync(id);
        return success
            ? Ok(new { code = 200, message = "操作成功" })
            : NotFound(new { code = 404, message = "组织单元不存在" });
    }
}
