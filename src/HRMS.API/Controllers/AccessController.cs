using HRMS.API.Services;
using HRMS.Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.API.Controllers;

[ApiController]
[Route("api/v1/access")]
public class AccessController : ControllerBase
{
    private readonly AccessControlService _service;
    private readonly CurrentUserAccessor _currentUserAccessor;

    public AccessController(AccessControlService service, CurrentUserAccessor currentUserAccessor)
    {
        _service = service;
        _currentUserAccessor = currentUserAccessor;
    }

    [HttpGet("menus/tree")]
    [RequirePermission("page.access.menu")]
    public async Task<IActionResult> GetMenuTree([FromQuery] bool includeButtons = true, [FromQuery] bool includeInactive = false)
    {
        return Ok(new { code = 200, message = "success", data = await _service.GetMenuTreeAsync(includeButtons, includeInactive) });
    }

    [HttpPost("menus")]
    [RequirePermission("button.access.menu.create")]
    public async Task<IActionResult> CreateMenu([FromBody] CreateMenuDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { code = 400, message = "验证失败", errors = GetErrors() });
        }

        return Ok(new { code = 200, message = "创建成功", data = await _service.CreateMenuAsync(dto) });
    }

    [HttpPut("menus")]
    [RequirePermission("button.access.menu.edit")]
    public async Task<IActionResult> UpdateMenu([FromBody] UpdateMenuDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { code = 400, message = "验证失败", errors = GetErrors() });
        }

        var result = await _service.UpdateMenuAsync(dto);
        return result == null
            ? NotFound(new { code = 404, message = "菜单不存在" })
            : Ok(new { code = 200, message = "更新成功", data = result });
    }

    [HttpDelete("menus/{id:guid}")]
    [RequirePermission("button.access.menu.delete")]
    public async Task<IActionResult> DeleteMenu(Guid id)
    {
        var success = await _service.DeleteMenuAsync(id);
        return success
            ? Ok(new { code = 200, message = "删除成功" })
            : NotFound(new { code = 404, message = "菜单不存在" });
    }

    [HttpGet("roles")]
    [RequirePermission("page.access.role")]
    public async Task<IActionResult> GetRoles()
    {
        return Ok(new { code = 200, message = "success", data = await _service.GetRolesAsync() });
    }

    [HttpGet("roles/{id:guid}")]
    [RequirePermission("page.access.role")]
    public async Task<IActionResult> GetRole(Guid id)
    {
        var result = await _service.GetRoleAsync(id);
        return result == null
            ? NotFound(new { code = 404, message = "角色不存在" })
            : Ok(new { code = 200, message = "success", data = result });
    }

    [HttpPost("roles")]
    [RequirePermission("button.access.role.create")]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { code = 400, message = "验证失败", errors = GetErrors() });
        }

        return Ok(new { code = 200, message = "创建成功", data = await _service.CreateRoleAsync(dto) });
    }

    [HttpPut("roles")]
    [RequirePermission("button.access.role.edit")]
    public async Task<IActionResult> UpdateRole([FromBody] UpdateRoleDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { code = 400, message = "验证失败", errors = GetErrors() });
        }

        var result = await _service.UpdateRoleAsync(dto);
        return result == null
            ? NotFound(new { code = 404, message = "角色不存在" })
            : Ok(new { code = 200, message = "更新成功", data = result });
    }

    [HttpDelete("roles/{id:guid}")]
    [RequirePermission("button.access.role.delete")]
    public async Task<IActionResult> DeleteRole(Guid id)
    {
        var success = await _service.DeleteRoleAsync(id);
        return success
            ? Ok(new { code = 200, message = "删除成功" })
            : NotFound(new { code = 404, message = "角色不存在" });
    }

    [HttpPut("roles/{id:guid}/permissions")]
    [RequirePermission("button.access.role.assign")]
    public async Task<IActionResult> AssignRolePermissions(Guid id, [FromBody] PermissionAssignmentDto dto)
    {
        await _service.AssignRolePermissionsAsync(id, dto.MenuIds);
        return Ok(new { code = 200, message = "权限分配成功" });
    }

    [HttpPut("roles/{id:guid}/permissions/copy")]
    [RequirePermission("button.access.role.assign")]
    public async Task<IActionResult> CopyRolePermissions(Guid id, [FromBody] CopyPermissionDto dto)
    {
        await _service.CopyRolePermissionsAsync(id, dto.SourceId);
        return Ok(new { code = 200, message = "角色权限复制成功" });
    }

    [HttpGet("posts")]
    [RequirePermission("page.access.post")]
    public async Task<IActionResult> GetPosts()
    {
        return Ok(new { code = 200, message = "success", data = await _service.GetPostsAsync() });
    }

    [HttpGet("posts/{id:guid}")]
    [RequirePermission("page.access.post")]
    public async Task<IActionResult> GetPost(Guid id)
    {
        var result = await _service.GetPostAsync(id);
        return result == null
            ? NotFound(new { code = 404, message = "岗位不存在" })
            : Ok(new { code = 200, message = "success", data = result });
    }

    [HttpPost("posts")]
    [RequirePermission("button.access.post.create")]
    public async Task<IActionResult> CreatePost([FromBody] CreatePostDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { code = 400, message = "验证失败", errors = GetErrors() });
        }

        return Ok(new { code = 200, message = "创建成功", data = await _service.CreatePostAsync(dto) });
    }

    [HttpPut("posts")]
    [RequirePermission("button.access.post.edit")]
    public async Task<IActionResult> UpdatePost([FromBody] UpdatePostDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { code = 400, message = "验证失败", errors = GetErrors() });
        }

        var result = await _service.UpdatePostAsync(dto);
        return result == null
            ? NotFound(new { code = 404, message = "岗位不存在" })
            : Ok(new { code = 200, message = "更新成功", data = result });
    }

    [HttpDelete("posts/{id:guid}")]
    [RequirePermission("button.access.post.delete")]
    public async Task<IActionResult> DeletePost(Guid id)
    {
        var success = await _service.DeletePostAsync(id);
        return success
            ? Ok(new { code = 200, message = "删除成功" })
            : NotFound(new { code = 404, message = "岗位不存在" });
    }

    [HttpPut("posts/{id:guid}/permissions")]
    [RequirePermission("button.access.post.assign")]
    public async Task<IActionResult> AssignPostPermissions(Guid id, [FromBody] PermissionAssignmentDto dto)
    {
        await _service.AssignPostPermissionsAsync(id, dto.MenuIds);
        return Ok(new { code = 200, message = "权限分配成功" });
    }

    [HttpPut("posts/{id:guid}/permissions/copy")]
    [RequirePermission("button.access.post.assign")]
    public async Task<IActionResult> CopyPostPermissions(Guid id, [FromBody] CopyPermissionDto dto)
    {
        await _service.CopyPostPermissionsAsync(id, dto.SourceId);
        return Ok(new { code = 200, message = "岗位权限复制成功" });
    }

    [HttpGet("users")]
    [RequirePermission("page.access.user")]
    public async Task<IActionResult> GetUsers()
    {
        return Ok(new { code = 200, message = "success", data = await _service.GetUsersAsync() });
    }

    [HttpGet("users/{id:guid}")]
    [RequirePermission("page.access.user")]
    public async Task<IActionResult> GetUser(Guid id)
    {
        var result = await _service.GetUserAsync(id);
        return result == null
            ? NotFound(new { code = 404, message = "用户不存在" })
            : Ok(new { code = 200, message = "success", data = result });
    }

    [HttpGet("users/{id:guid}/permissions")]
    [RequirePermission("page.access.user")]
    public async Task<IActionResult> GetUserPermissionContext(Guid id)
    {
        var result = await _service.GetUserPermissionContextAsync(id);
        return result == null
            ? NotFound(new { code = 404, message = "用户不存在" })
            : Ok(new { code = 200, message = "success", data = result });
    }

    [HttpPost("users")]
    [RequirePermission("button.access.user.create")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { code = 400, message = "验证失败", errors = GetErrors() });
        }

        return Ok(new { code = 200, message = "创建成功", data = await _service.CreateUserAsync(dto) });
    }

    [HttpPut("users")]
    [RequirePermission("button.access.user.edit")]
    public async Task<IActionResult> UpdateUser([FromBody] UpdateUserDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { code = 400, message = "验证失败", errors = GetErrors() });
        }

        var result = await _service.UpdateUserAsync(dto);
        return result == null
            ? NotFound(new { code = 404, message = "用户不存在" })
            : Ok(new { code = 200, message = "更新成功", data = result });
    }

    [HttpDelete("users/{id:guid}")]
    [RequirePermission("button.access.user.delete")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var currentUserId = await _currentUserAccessor.GetUserIdAsync();
        if (currentUserId == id)
        {
            return BadRequest(new { code = 400, message = "不能删除当前登录用户" });
        }

        var success = await _service.DeleteUserAsync(id);
        return success
            ? Ok(new { code = 200, message = "删除成功" })
            : NotFound(new { code = 404, message = "用户不存在" });
    }

    [HttpGet("options")]
    [RequirePermission("page.access.user")]
    public async Task<IActionResult> GetOptions()
    {
        var result = await _service.GetAccessOptionsAsync();
        return Ok(new
        {
            code = 200,
            message = "success",
            data = new
            {
                roles = result.Roles.Select(x => new AccessOptionItemDto { Id = x.Id, Name = x.RoleName }),
                posts = result.Posts.Select(x => new AccessOptionItemDto { Id = x.Id, Name = x.PostName })
            }
        });
    }

    private List<string> GetErrors()
    {
        return ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .ToList();
    }
}
