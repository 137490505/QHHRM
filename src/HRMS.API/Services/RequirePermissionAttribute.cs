using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HRMS.API.Services;

public class RequirePermissionAttribute : TypeFilterAttribute
{
    public RequirePermissionAttribute(string permissionCode) : base(typeof(PermissionActionFilter))
    {
        Arguments = new object[] { permissionCode };
    }
}

public class PermissionActionFilter : IAsyncActionFilter
{
    private readonly string _permissionCode;
    private readonly CurrentUserAccessor _currentUserAccessor;

    public PermissionActionFilter(string permissionCode, CurrentUserAccessor currentUserAccessor)
    {
        _permissionCode = permissionCode;
        _currentUserAccessor = currentUserAccessor;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!await _currentUserAccessor.UserExistsAsync())
        {
            context.Result = new UnauthorizedObjectResult(new { code = 401, message = "登录已失效，请重新登录" });
            return;
        }

        if (!await _currentUserAccessor.HasPermissionAsync(_permissionCode))
        {
            context.Result = new ObjectResult(new { code = 403, message = "无权限访问当前资源" })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
            return;
        }

        await next();
    }
}
