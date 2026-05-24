using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace HRMS.API.Services;

public interface ITodoCenterGateway
{
    Task<JsonElement> GetMyTasksAsync(
        string? assigneeId,
        string? status,
        string? businessType,
        string? taskTypeCode,
        string? businessId,
        string? processInstanceId,
        string? keyword,
        CancellationToken cancellationToken = default);

    Task<JsonElement> GetTaskDetailAsync(long id, CancellationToken cancellationToken = default);
    Task<JsonElement> CompleteTaskAsync(long id, TodoTaskActionDto request, CancellationToken cancellationToken = default);
    Task<JsonElement> RejectTaskAsync(long id, TodoTaskActionDto request, CancellationToken cancellationToken = default);
    Task<JsonElement> TransferTaskAsync(long id, TodoTaskTransferDto request, CancellationToken cancellationToken = default);
    Task<JsonElement> BatchCompleteAsync(TodoBatchCompleteDto request, CancellationToken cancellationToken = default);
    Task<JsonElement> BatchRejectAsync(TodoBatchRejectDto request, CancellationToken cancellationToken = default);
    Task<JsonElement> BatchTransferAsync(TodoBatchTransferDto request, CancellationToken cancellationToken = default);
    Task<JsonElement> BatchUrgeAsync(TodoBatchUrgeDto request, CancellationToken cancellationToken = default);
    Task<JsonElement> UrgeTaskAsync(long id, TodoTaskActionDto request, CancellationToken cancellationToken = default);
    Task<JsonElement> CreateAgentSettingAsync(CreateAgentSettingDto request, CancellationToken cancellationToken = default);
    Task<JsonElement> UpdateAgentSettingAsync(long id, UpdateAgentSettingDto request, CancellationToken cancellationToken = default);
    Task<JsonElement> DisableAgentSettingAsync(long id, CancellationToken cancellationToken = default);
    Task<JsonElement> DeleteAgentSettingAsync(long id, CancellationToken cancellationToken = default);
    Task<JsonElement> GetAgentSettingsAsync(string? principalUserId, CancellationToken cancellationToken = default);
    Task<JsonElement> GetKpiSummaryAsync(string? assigneeId, CancellationToken cancellationToken = default);
}

public interface IWorkflowProcessCenterGateway
{
    Task<JsonElement> GetDefinitionsAsync(CancellationToken cancellationToken = default);
    Task<JsonElement> GetDefinitionAsync(long id, CancellationToken cancellationToken = default);
    Task<JsonElement> SaveDefinitionAsync(long? id, SaveProcessDefinitionDto request, CancellationToken cancellationToken = default);
    Task<JsonElement> PublishDefinitionAsync(long id, CancellationToken cancellationToken = default);
    Task<JsonElement> StartProcessAsync(StartProcessDto request, CancellationToken cancellationToken = default);

    Task<JsonElement> GetInstancesAsync(
        string? processCode,
        string? status,
        string? businessType,
        string? businessId,
        string? keyword,
        CancellationToken cancellationToken = default);

    Task<JsonElement> GetInstanceAsync(long id, CancellationToken cancellationToken = default);
    Task<JsonElement> RejectInstanceAsync(long id, ProcessInstanceActionDto request, CancellationToken cancellationToken = default);
    Task<JsonElement> RebuildCurrentTodoAsync(long id, ProcessInstanceActionDto request, CancellationToken cancellationToken = default);
    Task<JsonElement> TerminateInstanceAsync(long id, ProcessInstanceActionDto request, CancellationToken cancellationToken = default);
    Task<JsonElement> GetCallbackLogsAsync(long id, CancellationToken cancellationToken = default);
    Task<JsonElement> RetryCallbackAsync(long id, long logId, CancellationToken cancellationToken = default);
}

public sealed class TodoCenterOptions
{
    public string BaseUrl { get; set; } = "http://localhost:5163";
}

public class TodoTaskActionDto
{
    public string? OperatorId { get; set; }
    public string? OperatorName { get; set; }
    public string? Comment { get; set; }
}

public sealed class TodoTaskTransferDto : TodoTaskActionDto
{
    public string TargetAssigneeId { get; set; } = string.Empty;
    public string TargetAssigneeName { get; set; } = string.Empty;
}

public sealed class TodoBatchCompleteDto : TodoTaskActionDto
{
    public List<long> TaskIds { get; set; } = [];
}

public sealed class TodoBatchRejectDto : TodoTaskActionDto
{
    public List<long> TaskIds { get; set; } = [];
}

public sealed class TodoBatchTransferDto : TodoTaskActionDto
{
    public List<long> TaskIds { get; set; } = [];
    public string TargetAssigneeId { get; set; } = string.Empty;
    public string TargetAssigneeName { get; set; } = string.Empty;
}

public sealed class TodoBatchUrgeDto : TodoTaskActionDto
{
    public List<long> TaskIds { get; set; } = [];
}

public sealed class CreateAgentSettingDto
{
    public string PrincipalUserId { get; set; } = string.Empty;
    public string AgentUserId { get; set; } = string.Empty;
    public string ScopeType { get; set; } = "All";
    public string? TaskTypeCode { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
}

public sealed class UpdateAgentSettingDto
{
    public string AgentUserId { get; set; } = string.Empty;
    public string ScopeType { get; set; } = "All";
    public string? TaskTypeCode { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
}

public sealed class ProcessInstanceActionDto
{
    public string? OperatorId { get; set; }
    public string? OperatorName { get; set; }
    public string? Comment { get; set; }
}

public sealed class SaveProcessDefinitionDto
{
    public string ProcessCode { get; set; } = string.Empty;
    public string ProcessName { get; set; } = string.Empty;
    public string? BusinessType { get; set; }
    public string? Description { get; set; }
    public int VersionNo { get; set; } = 1;
    public string DiagramXml { get; set; } = string.Empty;
    public bool IsPublished { get; set; } = false;
    public string? CallbackConfig { get; set; }
    public List<SaveProcessDefinitionNodeDto> Nodes { get; set; } = [];
}

public sealed class SaveProcessDefinitionNodeDto
{
    public string NodeId { get; set; } = string.Empty;
    public string NodeName { get; set; } = string.Empty;
    public string NodeType { get; set; } = "UserTask";
    public string AssigneeType { get; set; } = "User";
    public string AssigneeId { get; set; } = string.Empty;
    public string AssigneeName { get; set; } = string.Empty;
    public string? MultiPersonType { get; set; }
    public string? NextNodeId { get; set; }
    public List<SaveProcessNodeConditionDto>? Conditions { get; set; }
}

public sealed class SaveProcessNodeConditionDto
{
    public string? ConditionExpression { get; set; }
    public string TargetNodeId { get; set; } = string.Empty;
}

public sealed class StartProcessDto
{
    public string ProcessCode { get; set; } = string.Empty;
    public string BusinessSystem { get; set; } = string.Empty;
    public string BusinessType { get; set; } = string.Empty;
    public string BusinessId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string StarterId { get; set; } = string.Empty;
    public string StarterName { get; set; } = string.Empty;
    public string? RequestId { get; set; }
    public string? ExtData { get; set; }
}

public sealed class TodoCenterGateway(HttpClient httpClient, IOptions<TodoCenterOptions> options) : ITodoCenterGateway
{
    public Task<JsonElement> GetMyTasksAsync(
        string? assigneeId,
        string? status,
        string? businessType,
        string? taskTypeCode,
        string? businessId,
        string? processInstanceId,
        string? keyword,
        CancellationToken cancellationToken = default)
    {
        var path = WorkflowCenterPathBuilder.BuildPath(
            "/api/v1/todo/tasks/my-tasks",
            new Dictionary<string, string?>
            {
                ["assigneeId"] = assigneeId,
                ["status"] = status,
                ["businessType"] = businessType,
                ["taskTypeCode"] = taskTypeCode,
                ["businessId"] = businessId,
                ["processInstanceId"] = processInstanceId,
                ["keyword"] = keyword
            });
        return SendAsync(HttpMethod.Get, path, body: null, cancellationToken);
    }

    public Task<JsonElement> GetTaskDetailAsync(long id, CancellationToken cancellationToken = default)
        => SendAsync(HttpMethod.Get, $"/api/v1/todo/tasks/{id}/detail", body: null, cancellationToken);

    public Task<JsonElement> CompleteTaskAsync(long id, TodoTaskActionDto request, CancellationToken cancellationToken = default)
        => SendAsync(HttpMethod.Put, $"/api/v1/todo/tasks/{id}/complete", request, cancellationToken);

    public Task<JsonElement> RejectTaskAsync(long id, TodoTaskActionDto request, CancellationToken cancellationToken = default)
        => SendAsync(HttpMethod.Put, $"/api/v1/todo/tasks/{id}/reject", request, cancellationToken);

    public Task<JsonElement> TransferTaskAsync(long id, TodoTaskTransferDto request, CancellationToken cancellationToken = default)
        => SendAsync(HttpMethod.Put, $"/api/v1/todo/tasks/{id}/transfer", request, cancellationToken);

    public Task<JsonElement> BatchCompleteAsync(TodoBatchCompleteDto request, CancellationToken cancellationToken = default)
        => SendAsync(HttpMethod.Post, "/api/v1/todo/tasks/batch/complete", request, cancellationToken);

    public Task<JsonElement> BatchRejectAsync(TodoBatchRejectDto request, CancellationToken cancellationToken = default)
        => SendAsync(HttpMethod.Post, "/api/v1/todo/tasks/batch/reject", request, cancellationToken);

    public Task<JsonElement> BatchTransferAsync(TodoBatchTransferDto request, CancellationToken cancellationToken = default)
        => SendAsync(HttpMethod.Post, "/api/v1/todo/tasks/batch/transfer", request, cancellationToken);

    public Task<JsonElement> BatchUrgeAsync(TodoBatchUrgeDto request, CancellationToken cancellationToken = default)
        => SendAsync(HttpMethod.Post, "/api/v1/todo/tasks/batch/urge", request, cancellationToken);

    public Task<JsonElement> UrgeTaskAsync(long id, TodoTaskActionDto request, CancellationToken cancellationToken = default)
        => SendAsync(HttpMethod.Post, $"/api/v1/todo/tasks/{id}/urge", request, cancellationToken);

    public Task<JsonElement> CreateAgentSettingAsync(CreateAgentSettingDto request, CancellationToken cancellationToken = default)
        => SendAsync(HttpMethod.Post, "/api/v1/todo/agent/settings", request, cancellationToken);

    public Task<JsonElement> UpdateAgentSettingAsync(long id, UpdateAgentSettingDto request, CancellationToken cancellationToken = default)
        => SendAsync(HttpMethod.Put, $"/api/v1/todo/agent/settings/{id}", request, cancellationToken);

    public Task<JsonElement> DisableAgentSettingAsync(long id, CancellationToken cancellationToken = default)
        => SendAsync(HttpMethod.Put, $"/api/v1/todo/agent/settings/{id}/disable", body: null, cancellationToken);

    public Task<JsonElement> DeleteAgentSettingAsync(long id, CancellationToken cancellationToken = default)
        => SendAsync(HttpMethod.Delete, $"/api/v1/todo/agent/settings/{id}", body: null, cancellationToken);

    public Task<JsonElement> GetAgentSettingsAsync(string? principalUserId, CancellationToken cancellationToken = default)
    {
        var path = WorkflowCenterPathBuilder.BuildPath(
            "/api/v1/todo/agent/settings",
            new Dictionary<string, string?>
            {
                ["principalUserId"] = principalUserId
            });
        return SendAsync(HttpMethod.Get, path, body: null, cancellationToken);
    }

    public Task<JsonElement> GetKpiSummaryAsync(string? assigneeId, CancellationToken cancellationToken = default)
    {
        var path = WorkflowCenterPathBuilder.BuildPath(
            "/api/v1/todo/kpi/summary",
            new Dictionary<string, string?>
            {
                ["assigneeId"] = assigneeId
            });
        return SendAsync(HttpMethod.Get, path, body: null, cancellationToken);
    }

    private async Task<JsonElement> SendAsync(HttpMethod method, string path, object? body, CancellationToken cancellationToken)
    {
        WorkflowCenterPathBuilder.EnsureBaseAddress(httpClient, options.Value.BaseUrl, "TodoCenter");

        using var request = new HttpRequestMessage(method, path);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        using var response = await httpClient.SendAsync(request, cancellationToken);
        return await WorkflowCenterResponseParser.ReadDataAsync(response, "TodoCenter", cancellationToken);
    }
}

public sealed class WorkflowProcessCenterGateway(HttpClient httpClient, IOptions<ProcessCenterOptions> options) : IWorkflowProcessCenterGateway
{
    public Task<JsonElement> GetDefinitionsAsync(CancellationToken cancellationToken = default)
        => SendAsync(HttpMethod.Get, "/api/v1/process/definitions", body: null, cancellationToken);

    public Task<JsonElement> GetDefinitionAsync(long id, CancellationToken cancellationToken = default)
        => SendAsync(HttpMethod.Get, $"/api/v1/process/definitions/{id}", body: null, cancellationToken);

    public Task<JsonElement> SaveDefinitionAsync(long? id, SaveProcessDefinitionDto request, CancellationToken cancellationToken = default)
        => id.HasValue
            ? SendAsync(HttpMethod.Put, $"/api/v1/process/definitions/{id}", request, cancellationToken)
            : SendAsync(HttpMethod.Post, "/api/v1/process/definitions", request, cancellationToken);

    public Task<JsonElement> PublishDefinitionAsync(long id, CancellationToken cancellationToken = default)
        => SendAsync(HttpMethod.Put, $"/api/v1/process/definitions/{id}/publish", body: null, cancellationToken);

    public Task<JsonElement> StartProcessAsync(StartProcessDto request, CancellationToken cancellationToken = default)
        => SendAsync(HttpMethod.Post, "/api/v1/process/start", request, cancellationToken);

    public Task<JsonElement> GetInstancesAsync(
        string? processCode,
        string? status,
        string? businessType,
        string? businessId,
        string? keyword,
        CancellationToken cancellationToken = default)
    {
        var path = WorkflowCenterPathBuilder.BuildPath(
            "/api/v1/process/instances",
            new Dictionary<string, string?>
            {
                ["processCode"] = processCode,
                ["status"] = status,
                ["businessType"] = businessType,
                ["businessId"] = businessId,
                ["keyword"] = keyword
            });
        return SendAsync(HttpMethod.Get, path, body: null, cancellationToken);
    }

    public Task<JsonElement> GetInstanceAsync(long id, CancellationToken cancellationToken = default)
        => SendAsync(HttpMethod.Get, $"/api/v1/process/instance/{id}", body: null, cancellationToken);

    public Task<JsonElement> RejectInstanceAsync(long id, ProcessInstanceActionDto request, CancellationToken cancellationToken = default)
        => SendAsync(HttpMethod.Post, $"/api/v1/process/instance/{id}/reject", request, cancellationToken);

    public Task<JsonElement> RebuildCurrentTodoAsync(long id, ProcessInstanceActionDto request, CancellationToken cancellationToken = default)
        => SendAsync(HttpMethod.Post, $"/api/v1/process/instance/{id}/rebuild-current-todo", request, cancellationToken);

    public Task<JsonElement> TerminateInstanceAsync(long id, ProcessInstanceActionDto request, CancellationToken cancellationToken = default)
        => SendAsync(HttpMethod.Post, $"/api/v1/process/instance/{id}/terminate", request, cancellationToken);

    public Task<JsonElement> GetCallbackLogsAsync(long id, CancellationToken cancellationToken = default)
        => SendAsync(HttpMethod.Get, $"/api/v1/process/instance/{id}/callback-logs", body: null, cancellationToken);

    public Task<JsonElement> RetryCallbackAsync(long id, long logId, CancellationToken cancellationToken = default)
        => SendAsync(HttpMethod.Post, $"/api/v1/process/instance/{id}/callback-logs/{logId}/retry", body: null, cancellationToken);

    private async Task<JsonElement> SendAsync(HttpMethod method, string path, object? body, CancellationToken cancellationToken)
    {
        WorkflowCenterPathBuilder.EnsureBaseAddress(httpClient, options.Value.BaseUrl, "ProcessCenter");

        using var request = new HttpRequestMessage(method, path);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        using var response = await httpClient.SendAsync(request, cancellationToken);
        return await WorkflowCenterResponseParser.ReadDataAsync(response, "ProcessCenter", cancellationToken);
    }
}

public sealed class RemoteServiceException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

internal static class WorkflowCenterResponseParser
{
    public static async Task<JsonElement> ReadDataAsync(HttpResponseMessage response, string serviceName, CancellationToken cancellationToken)
    {
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        var hasJson = TryParse(raw, out var document);

        using (document)
        {
            // #region debug-point B:response-parser-entry
            _ = Task.Run(async () => { try { using var c = new HttpClient(); await c.PostAsJsonAsync("http://127.0.0.1:7777/event", new { sessionId = "todo-null-object", runId = "pre-fix", hypothesisId = "B", location = "WorkflowCenterGateway.ReadDataAsync:274", msg = "[DEBUG] workflow center raw response parsed", data = new { serviceName, statusCode = (int)response.StatusCode, hasJson, rootKind = document?.RootElement.ValueKind.ToString(), rawPreview = string.IsNullOrWhiteSpace(raw) ? "<empty>" : raw[..Math.Min(raw.Length, 300)] }, ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }, cancellationToken); } catch { } }, cancellationToken);
            // #endregion
            if (!response.IsSuccessStatusCode)
            {
                var root = document?.RootElement;
                var message = hasJson && IsObject(root) && root!.Value.TryGetProperty("message", out var messageElement)
                    ? messageElement.GetString()
                    : null;
                throw new RemoteServiceException(
                    (int)response.StatusCode,
                    message ?? $"{serviceName} 调用失败，状态码：{(int)response.StatusCode}");
            }

            // #region debug-point A:before-read-data
            _ = Task.Run(async () => { try { using var c = new HttpClient(); await c.PostAsJsonAsync("http://127.0.0.1:7777/event", new { sessionId = "todo-null-object", runId = "pre-fix", hypothesisId = "A", location = "WorkflowCenterGateway.ReadDataAsync:289", msg = "[DEBUG] workflow center about to read data property", data = new { serviceName, hasJson, documentIsNull = document is null, rootKind = document?.RootElement.ValueKind.ToString() }, ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }, cancellationToken); } catch { } }, cancellationToken);
            // #endregion
            if (!hasJson || document is null || document.RootElement.ValueKind != JsonValueKind.Object || !document.RootElement.TryGetProperty("data", out var dataElement))
            {
                return JsonDocument.Parse("null").RootElement.Clone();
            }

            // #region debug-point C:data-element-kind
            _ = Task.Run(async () => { try { using var c = new HttpClient(); await c.PostAsJsonAsync("http://127.0.0.1:7777/event", new { sessionId = "todo-null-object", runId = "pre-fix", hypothesisId = "C", location = "WorkflowCenterGateway.ReadDataAsync:294", msg = "[DEBUG] workflow center data property extracted", data = new { serviceName, dataKind = dataElement.ValueKind.ToString() }, ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }, cancellationToken); } catch { } }, cancellationToken);
            // #endregion
            return dataElement.Clone();
        }
    }

    private static bool TryParse(string raw, out JsonDocument? document)
    {
        try
        {
            document = JsonDocument.Parse(string.IsNullOrWhiteSpace(raw) ? "null" : raw);
            return true;
        }
        catch (JsonException)
        {
            document = null;
            return false;
        }
    }

    private static bool IsObject(JsonElement? element)
        => element.HasValue && element.Value.ValueKind == JsonValueKind.Object;
}

internal static class WorkflowCenterPathBuilder
{
    public static string BuildPath(string path, IDictionary<string, string?> query)
    {
        var filteredQuery = query
            .Where(item => !string.IsNullOrWhiteSpace(item.Value))
            .ToDictionary(item => item.Key, item => (string?)item.Value);

        return filteredQuery.Count == 0
            ? path
            : QueryHelpers.AddQueryString(path, filteredQuery);
    }

    public static void EnsureBaseAddress(HttpClient httpClient, string? baseUrl, string serviceName)
    {
        if (httpClient.BaseAddress is not null)
        {
            return;
        }

        var normalizedBaseUrl = baseUrl?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedBaseUrl))
        {
            throw new InvalidOperationException($"{serviceName} BaseUrl 未配置。");
        }

        httpClient.BaseAddress = new Uri(normalizedBaseUrl, UriKind.Absolute);
    }
}
