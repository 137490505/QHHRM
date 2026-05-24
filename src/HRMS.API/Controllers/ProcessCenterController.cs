using System.Text.Json;
using HRMS.API.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace HRMS.API.Controllers;

[ApiController]
[Route("api/v1/process")]
public class ProcessCenterController : ControllerBase
{
    private readonly IWorkflowProcessCenterGateway _gateway;

    public ProcessCenterController(IWorkflowProcessCenterGateway gateway)
    {
        _gateway = gateway;
    }

    [HttpGet("definitions")]
    [RequirePermission("page.process.center")]
    public Task<IActionResult> GetDefinitions(CancellationToken cancellationToken)
    {
        return ExecuteAsync(() => _gateway.GetDefinitionsAsync(cancellationToken), "success");
    }

    [HttpGet("definitions/{id:long}")]
    [RequirePermission("page.process.center")]
    public Task<IActionResult> GetDefinition(long id, CancellationToken cancellationToken)
    {
        return ExecuteAsync(() => _gateway.GetDefinitionAsync(id, cancellationToken), "success");
    }

    [HttpPost("definitions")]
    [RequirePermission("page.process.designer")]
    public Task<IActionResult> CreateDefinition([FromBody] SaveProcessDefinitionDto request, CancellationToken cancellationToken)
    {
        return ExecuteAsync(() => _gateway.SaveDefinitionAsync(null, request, cancellationToken), "流程定义已创建");
    }

    [HttpPut("definitions/{id:long}")]
    [RequirePermission("page.process.designer")]
    public Task<IActionResult> UpdateDefinition(long id, [FromBody] SaveProcessDefinitionDto request, CancellationToken cancellationToken)
    {
        return ExecuteAsync(() => _gateway.SaveDefinitionAsync(id, request, cancellationToken), "流程定义已更新");
    }

    [HttpPut("definitions/{id:long}/publish")]
    [RequirePermission("page.process.designer")]
    public Task<IActionResult> PublishDefinition(long id, CancellationToken cancellationToken)
    {
        return ExecuteAsync(() => _gateway.PublishDefinitionAsync(id, cancellationToken), "流程定义已发布");
    }

    [HttpPost("start")]
    [RequirePermission("page.process.requests")]
    public Task<IActionResult> StartProcess([FromBody] StartProcessDto request, CancellationToken cancellationToken)
    {
        return ExecuteAsync(() => _gateway.StartProcessAsync(request, cancellationToken), "流程启动成功");
    }

    [HttpGet("instances")]
    [RequirePermission("page.process.center")]
    public Task<IActionResult> GetInstances(
        [FromQuery] string? processCode,
        [FromQuery] string? status,
        [FromQuery] string? businessType,
        [FromQuery] string? businessId,
        [FromQuery] string? keyword,
        CancellationToken cancellationToken)
    {
        return ExecuteAsync(
            () => _gateway.GetInstancesAsync(processCode, status, businessType, businessId, keyword, cancellationToken),
            "success");
    }

    [HttpGet("instances/{id:long}")]
    [RequirePermission("page.process.center")]
    public Task<IActionResult> GetInstance(long id, CancellationToken cancellationToken)
    {
        return ExecuteAsync(() => _gateway.GetInstanceAsync(id, cancellationToken), "success");
    }

    [HttpPost("instances/{id:long}/reject")]
    [RequirePermission("button.process.reject")]
    public Task<IActionResult> RejectInstance(long id, [FromBody] ProcessInstanceActionDto request, CancellationToken cancellationToken)
    {
        return ExecuteAsync(() => _gateway.RejectInstanceAsync(id, request, cancellationToken), "流程已驳回");
    }

    [HttpPost("instances/{id:long}/rebuild-current-todo")]
    [RequirePermission("button.process.rebuildTodo")]
    public Task<IActionResult> RebuildCurrentTodo(long id, [FromBody] ProcessInstanceActionDto request, CancellationToken cancellationToken)
    {
        return ExecuteAsync(() => _gateway.RebuildCurrentTodoAsync(id, request, cancellationToken), "当前待办已重建");
    }

    [HttpPost("instances/{id:long}/terminate")]
    [RequirePermission("button.process.terminate")]
    public Task<IActionResult> TerminateInstance(long id, [FromBody] ProcessInstanceActionDto request, CancellationToken cancellationToken)
    {
        return ExecuteAsync(() => _gateway.TerminateInstanceAsync(id, request, cancellationToken), "流程已终止");
    }

    [HttpGet("instances/{id:long}/callback-logs")]
    [RequirePermission("page.process.center")]
    public Task<IActionResult> GetCallbackLogs(long id, CancellationToken cancellationToken)
    {
        return ExecuteAsync(() => _gateway.GetCallbackLogsAsync(id, cancellationToken), "success");
    }

    [HttpPost("instances/{id:long}/callback-logs/{logId:long}/retry")]
    [RequirePermission("button.process.terminate")]
    public Task<IActionResult> RetryCallback(long id, long logId, CancellationToken cancellationToken)
    {
        return ExecuteAsync(() => _gateway.RetryCallbackAsync(id, logId, cancellationToken), "回调已重试");
    }

    private async Task<IActionResult> ExecuteAsync(Func<Task<JsonElement>> action, string message)
    {
        try
        {
            var data = await action();
            var payload = data.ValueKind == JsonValueKind.Null
                ? null
                : JsonConvert.DeserializeObject<object>(data.GetRawText());
            return Ok(new { code = 200, message, data = payload });
        }
        catch (RemoteServiceException exception)
        {
            return StatusCode(exception.StatusCode, new { code = exception.StatusCode, message = exception.Message });
        }
    }
}
