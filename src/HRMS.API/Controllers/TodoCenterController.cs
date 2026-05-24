using System.Text.Json;
using HRMS.API.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace HRMS.API.Controllers;

[ApiController]
[Route("api/v1/todo")]
public class TodoCenterController : ControllerBase
{
    private readonly ITodoCenterGateway _gateway;

    public TodoCenterController(ITodoCenterGateway gateway)
    {
        _gateway = gateway;
    }

    [HttpGet("tasks/my-tasks")]
    [RequirePermission("page.todo.center")]
    public Task<IActionResult> GetMyTasks(
        [FromQuery] string? assigneeId,
        [FromQuery] string? status,
        [FromQuery] string? businessType,
        [FromQuery] string? taskTypeCode,
        [FromQuery] string? businessId,
        [FromQuery] string? processInstanceId,
        [FromQuery] string? keyword,
        CancellationToken cancellationToken)
    {
        return ExecuteAsync(
            () => _gateway.GetMyTasksAsync(assigneeId, status, businessType, taskTypeCode, businessId, processInstanceId, keyword, cancellationToken),
            "success");
    }

    [HttpGet("tasks/{id:long}/detail")]
    [RequirePermission("page.todo.center")]
    public Task<IActionResult> GetTaskDetail(long id, CancellationToken cancellationToken)
    {
        return ExecuteAsync(() => _gateway.GetTaskDetailAsync(id, cancellationToken), "success");
    }

    [HttpPut("tasks/{id:long}/complete")]
    [RequirePermission("button.todo.complete")]
    public Task<IActionResult> CompleteTask(long id, [FromBody] TodoTaskActionDto request, CancellationToken cancellationToken)
    {
        return ExecuteAsync(() => _gateway.CompleteTaskAsync(id, request, cancellationToken), "任务处理成功");
    }

    [HttpPut("tasks/{id:long}/reject")]
    [RequirePermission("button.todo.reject")]
    public Task<IActionResult> RejectTask(long id, [FromBody] TodoTaskActionDto request, CancellationToken cancellationToken)
    {
        return ExecuteAsync(() => _gateway.RejectTaskAsync(id, request, cancellationToken), "任务驳回成功");
    }

    [HttpPut("tasks/{id:long}/transfer")]
    [RequirePermission("button.todo.transfer")]
    public Task<IActionResult> TransferTask(long id, [FromBody] TodoTaskTransferDto request, CancellationToken cancellationToken)
    {
        return ExecuteAsync(() => _gateway.TransferTaskAsync(id, request, cancellationToken), "任务转交成功");
    }

    [HttpPost("tasks/batch/complete")]
    [RequirePermission("button.todo.batchComplete")]
    public Task<IActionResult> BatchComplete([FromBody] TodoBatchCompleteDto request, CancellationToken cancellationToken)
    {
        return ExecuteAsync(() => _gateway.BatchCompleteAsync(request, cancellationToken), "批量处理成功");
    }

    [HttpPost("tasks/batch/reject")]
    [RequirePermission("button.todo.batchReject")]
    public Task<IActionResult> BatchReject([FromBody] TodoBatchRejectDto request, CancellationToken cancellationToken)
    {
        return ExecuteAsync(() => _gateway.BatchRejectAsync(request, cancellationToken), "批量驳回成功");
    }

    [HttpPost("tasks/batch/transfer")]
    [RequirePermission("button.todo.transfer")]
    public Task<IActionResult> BatchTransfer([FromBody] TodoBatchTransferDto request, CancellationToken cancellationToken)
    {
        return ExecuteAsync(() => _gateway.BatchTransferAsync(request, cancellationToken), "批量转交成功");
    }

    [HttpPost("tasks/batch/urge")]
    [RequirePermission("button.todo.urge")]
    public Task<IActionResult> BatchUrge([FromBody] TodoBatchUrgeDto request, CancellationToken cancellationToken)
    {
        return ExecuteAsync(() => _gateway.BatchUrgeAsync(request, cancellationToken), "批量催办成功");
    }

    [HttpPost("tasks/{id:long}/urge")]
    [RequirePermission("button.todo.urge")]
    public Task<IActionResult> UrgeTask(long id, [FromBody] TodoTaskActionDto request, CancellationToken cancellationToken)
    {
        return ExecuteAsync(() => _gateway.UrgeTaskAsync(id, request, cancellationToken), "催办成功");
    }

    [HttpPost("agent/settings")]
    [RequirePermission("button.todo.agent")]
    public Task<IActionResult> CreateAgentSetting([FromBody] CreateAgentSettingDto request, CancellationToken cancellationToken)
    {
        return ExecuteAsync(() => _gateway.CreateAgentSettingAsync(request, cancellationToken), "代理规则创建成功");
    }

    [HttpPut("agent/settings/{id:long}")]
    [RequirePermission("button.todo.agent")]
    public Task<IActionResult> UpdateAgentSetting(long id, [FromBody] UpdateAgentSettingDto request, CancellationToken cancellationToken)
    {
        return ExecuteAsync(() => _gateway.UpdateAgentSettingAsync(id, request, cancellationToken), "代理规则更新成功");
    }

    [HttpPut("agent/settings/{id:long}/disable")]
    [RequirePermission("button.todo.agent")]
    public Task<IActionResult> DisableAgentSetting(long id, CancellationToken cancellationToken)
    {
        return ExecuteAsync(() => _gateway.DisableAgentSettingAsync(id, cancellationToken), "代理规则已停用");
    }

    [HttpDelete("agent/settings/{id:long}")]
    [RequirePermission("button.todo.agent")]
    public Task<IActionResult> DeleteAgentSetting(long id, CancellationToken cancellationToken)
    {
        return ExecuteAsync(() => _gateway.DeleteAgentSettingAsync(id, cancellationToken), "代理规则已删除");
    }

    [HttpGet("agent/settings")]
    [RequirePermission("page.todo.center")]
    public Task<IActionResult> GetAgentSettings([FromQuery] string? principalUserId, CancellationToken cancellationToken)
    {
        return ExecuteAsync(() => _gateway.GetAgentSettingsAsync(principalUserId, cancellationToken), "success");
    }

    [HttpGet("kpi/summary")]
    [RequirePermission("page.todo.center")]
    public Task<IActionResult> GetKpiSummary([FromQuery] string? assigneeId, CancellationToken cancellationToken)
    {
        return ExecuteAsync(() => _gateway.GetKpiSummaryAsync(assigneeId, cancellationToken), "success");
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
