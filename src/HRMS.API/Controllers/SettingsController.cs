using HRMS.API.Services;
using HRMS.Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.API.Controllers;

[ApiController]
[Route("api/v1/settings")]
public class SettingsController : ControllerBase
{
    private readonly SettingsService _settingsService;

    public SettingsController(SettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    [HttpGet("login-security")]
    [RequirePermission("page.settings")]
    public async Task<IActionResult> GetLoginSecuritySettings()
    {
        return Ok(new { code = 200, message = "success", data = await _settingsService.GetLoginSecuritySettingsAsync() });
    }

    [HttpPut("login-security")]
    [RequirePermission("page.settings")]
    public async Task<IActionResult> UpdateLoginSecuritySettings([FromBody] UpdateLoginSecuritySettingsDto dto)
    {
        return Ok(new { code = 200, message = "保存成功", data = await _settingsService.UpdateLoginSecuritySettingsAsync(dto) });
    }

    [HttpGet("select-options/{category}")]
    [RequirePermission("page.settings")]
    public async Task<IActionResult> GetSelectOptions(string category)
    {
        return Ok(new { code = 200, message = "success", data = await _settingsService.GetSelectOptionsAsync(category) });
    }

    [HttpPut("select-options/{category}")]
    [RequirePermission("page.settings")]
    public async Task<IActionResult> UpdateSelectOptions(string category, [FromBody] UpdateSelectOptionsDto dto)
    {
        return Ok(new { code = 200, message = "保存成功", data = await _settingsService.UpdateSelectOptionsAsync(category, dto) });
    }
}
