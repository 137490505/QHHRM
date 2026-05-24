using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using ProcessCenter.API.Contracts;

namespace ProcessCenter.API.Services;

public interface ITodoCenterClient
{
    Task<TodoTaskDispatchResult> CreateTaskAsync(ProcessTodoCommandDto command, CancellationToken cancellationToken = default);
    Task CancelByProcessNodeAsync(
        string processInstanceId,
        string processNodeId,
        string targetStatus,
        string? operatorId,
        string? operatorName,
        string? comment,
        CancellationToken cancellationToken = default);
}

public sealed class TodoCenterOptions
{
    public string BaseUrl { get; set; } = "http://localhost:5163";
}

public sealed class TodoCenterHttpClient(HttpClient httpClient, IOptions<TodoCenterOptions> options) : ITodoCenterClient
{
    public async Task<TodoTaskDispatchResult> CreateTaskAsync(ProcessTodoCommandDto command, CancellationToken cancellationToken = default)
    {
        if (httpClient.BaseAddress is null)
        {
            var baseUrl = options.Value.BaseUrl?.Trim();
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                throw new InvalidOperationException("TodoCenter BaseUrl 未配置。");
            }

            httpClient.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
        }

        var request = new TodoCenterCreateTaskRequest
        {
            TaskTypeCode = command.TaskTypeCode,
            Title = command.Title,
            BusinessSystem = command.BusinessSystem,
            BusinessType = command.BusinessType,
            BusinessId = command.BusinessId,
            ProcessInstanceId = command.ProcessInstanceId,
            ProcessNodeId = command.ProcessNodeId,
            AssigneeId = command.AssigneeId,
            AssigneeName = command.AssigneeName,
            Priority = command.Priority,
            SourcePayload = command.SourcePayload,
            CreatedBy = command.CreatedBy
        };

        using var response = await httpClient.PostAsJsonAsync("/api/todo/tasks", request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new TodoCenterClientException(
                $"调用 TodoCenter 创建待办失败，状态码：{(int)response.StatusCode}，响应：{responseText}");
        }

        var envelope = await response.Content.ReadFromJsonAsync<TodoCenterEnvelope<TodoCenterTaskData>>(cancellationToken: cancellationToken);
        var data = envelope?.Data ?? throw new TodoCenterClientException("TodoCenter 返回内容缺少 data。");

        return new TodoTaskDispatchResult
        {
            Id = data.Id,
            TaskNo = data.TaskNo,
            Status = data.Status,
            AssigneeId = data.AssigneeId,
            AssigneeName = data.AssigneeName
        };
    }

    public async Task CancelByProcessNodeAsync(
        string processInstanceId,
        string processNodeId,
        string targetStatus,
        string? operatorId,
        string? operatorName,
        string? comment,
        CancellationToken cancellationToken = default)
    {
        if (httpClient.BaseAddress is null)
        {
            var baseUrl = options.Value.BaseUrl?.Trim();
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                throw new InvalidOperationException("TodoCenter BaseUrl 未配置。");
            }

            httpClient.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
        }

        var request = new TodoCenterCancelByProcessNodeRequest
        {
            ProcessInstanceId = processInstanceId,
            ProcessNodeId = processNodeId,
            TargetStatus = targetStatus,
            OperatorId = operatorId,
            OperatorName = operatorName,
            Comment = comment
        };

        using var response = await httpClient.PostAsJsonAsync("/api/todo/tasks/cancel/by-process-node", request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new TodoCenterClientException(
                $"调用 TodoCenter 取消待办失败，状态码：{(int)response.StatusCode}，响应：{responseText}");
        }
    }
}

public sealed class TodoCenterClientException(string message) : Exception(message)
{
}

public sealed class TodoTaskDispatchResult
{
    public long Id { get; set; }
    public string TaskNo { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string AssigneeId { get; set; } = string.Empty;
    public string AssigneeName { get; set; } = string.Empty;
}

internal sealed class TodoCenterCreateTaskRequest
{
    public string TaskTypeCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string BusinessSystem { get; set; } = string.Empty;
    public string BusinessType { get; set; } = string.Empty;
    public string BusinessId { get; set; } = string.Empty;
    public string ProcessInstanceId { get; set; } = string.Empty;
    public string ProcessNodeId { get; set; } = string.Empty;
    public string AssigneeId { get; set; } = string.Empty;
    public string AssigneeName { get; set; } = string.Empty;
    public int Priority { get; set; }
    public string? SourcePayload { get; set; }
    public string? CreatedBy { get; set; }
}

internal sealed class TodoCenterCancelByProcessNodeRequest
{
    public string ProcessInstanceId { get; set; } = string.Empty;
    public string ProcessNodeId { get; set; } = string.Empty;
    public string TargetStatus { get; set; } = "Canceled";
    public string? OperatorId { get; set; }
    public string? OperatorName { get; set; }
    public string? Comment { get; set; }
}

internal sealed class TodoCenterEnvelope<T>
{
    public int Code { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public string TraceId { get; set; } = string.Empty;
}

internal sealed class TodoCenterTaskData
{
    public long Id { get; set; }
    public string TaskNo { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("assigneeId")]
    public string AssigneeId { get; set; } = string.Empty;

    [JsonPropertyName("assigneeName")]
    public string AssigneeName { get; set; } = string.Empty;
}
