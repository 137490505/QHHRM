using HRMS.API.Services;
using HRMS.Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AccessControlService _accessControlService;
    private readonly CurrentUserAccessor _currentUserAccessor;
    private readonly SettingsService _settingsService;

    public AuthController(AccessControlService accessControlService, CurrentUserAccessor currentUserAccessor, SettingsService settingsService)
    {
        _accessControlService = accessControlService;
        _currentUserAccessor = currentUserAccessor;
        _settingsService = settingsService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest(new { code = 400, message = "用户名和密码不能为空" });
            }

            var captchaEnabled = await _settingsService.IsLoginCaptchaEnabledAsync();

            if (captchaEnabled)
            {
                string? captchaCode = HttpContext.Session.GetString("CaptchaCode");
                if (string.IsNullOrEmpty(captchaCode))
                {
                    return BadRequest(new { code = 400, message = "验证码已过期，请刷新" });
                }

                if (!string.Equals(captchaCode, request.Captcha, StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new { code = 400, message = "验证码错误" });
                }
            }

            var result = await _accessControlService.LoginAsync(request.Username, request.Password);
            if (result != null)
            {
                HttpContext.Session.Remove("CaptchaCode");
                return Ok(new { code = 200, message = "登录成功", data = result });
            }
        }
        catch
        {
            throw;
        }

        return BadRequest(new { code = 401, message = "用户名或密码错误" });
    }

    [HttpGet("login-options")]
    public async Task<IActionResult> GetLoginOptions()
    {
        return Ok(new
        {
            code = 200,
            message = "success",
            data = await _settingsService.GetLoginSecuritySettingsAsync()
        });
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var userId = await _currentUserAccessor.GetUserIdAsync();
        var token = _currentUserAccessor.GetToken();
        if (!userId.HasValue || string.IsNullOrWhiteSpace(token))
        {
            return Unauthorized(new { code = 401, message = "登录已失效，请重新登录" });
        }

        var result = await _accessControlService.GetCurrentContextAsync(userId.Value, token);
        return result == null
            ? Unauthorized(new { code = 401, message = "登录已失效，请重新登录" })
            : Ok(new { code = 200, message = "success", data = result });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var token = _currentUserAccessor.GetToken();
        if (!string.IsNullOrWhiteSpace(token))
        {
            await _accessControlService.LogoutAsync(token);
        }
        HttpContext.Session.Clear();
        return Ok(new { code = 200, message = "退出成功" });
    }
}
