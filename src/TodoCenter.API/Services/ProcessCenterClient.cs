using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace TodoCenter.API.Services;

public interface IProcessCenterClient
{
    Task CompleteTaskAsync(string processInstanceId, string? operatorId, string? operatorName, string? comment, CancellationToken cancellationToken = default);
    Task RejectTaskAsync(string processInstanceId, string? operatorId, string? operatorName, string? comment, CancellationToken cancellationToken = default);
}

public sealed class ProcessCenterOptions
{
    public string BaseUrl { get; set; } = "http://localhost:5099";
}

public sealed class ProcessCenterHttpClient(
    HttpClient httpClient,
    IOptions<ProcessCenterOptions> options,
    IOptions<InternalCallbackOptions> callbackOptions) : IProcessCenterClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public Task CompleteTaskAsync(string processInstanceId, string? operatorId, string? operatorName, string? comment, CancellationToken cancellationToken = default)
    {
        return SendTaskActionAsync(processInstanceId, "Complete", operatorId, operatorName, comment, cancellationToken);
    }

    public Task RejectTaskAsync(string processInstanceId, string? operatorId, string? operatorName, string? comment, CancellationToken cancellationToken = default)
    {
        return SendTaskActionAsync(processInstanceId, "Reject", operatorId, operatorName, comment, cancellationToken);
    }

    private async Task SendTaskActionAsync(
        string processInstanceId,
        string action,
        string? operatorId,
        string? operatorName,
        string? comment,
        CancellationToken cancellationToken)
    {
        if (!long.TryParse(processInstanceId, out var parsedProcessInstanceId))
        {
            throw new ProcessCenterClientException($"流程实例 ID 格式无效：{processInstanceId}");
        }

        if (httpClient.BaseAddress is null)
        {
            var baseUrl = options.Value.BaseUrl?.Trim();
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                throw new InvalidOperationException("ProcessCenter BaseUrl 未配置。");
            }

            httpClient.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
        }

        var request = new ProcessTaskActionRequest
        {
            ProcessInstanceId = parsedProcessInstanceId,
            Action = action,
            OperatorId = operatorId,
            OperatorName = operatorName,
            Comment = comment
        };

        const string path = "/api/v1/process/complete-task";
        var payloadJson = JsonSerializer.Serialize(request, SerializerOptions);
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(request, options: SerializerOptions)
        };
        AddCallbackHeaders(httpRequest, path, payloadJson);

        using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new ProcessCenterClientException(
                $"调用 ProcessCenter 处理待办失败，状态码：{(int)response.StatusCode}，响应：{responseText}");
        }
    }

    private void AddCallbackHeaders(HttpRequestMessage request, string path, string payloadJson)
    {
        var eventId = Guid.NewGuid().ToString("N");
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var signature = CallbackSecurityService.CreateSignature(
            callbackOptions.Value.SharedSecret,
            eventId,
            timestamp,
            path,
            payloadJson);

        request.Headers.Add(CallbackSecurityHeaders.EventId, eventId);
        request.Headers.Add(CallbackSecurityHeaders.Timestamp, timestamp);
        request.Headers.Add(CallbackSecurityHeaders.Signature, signature);
    }
}

public sealed class ProcessCenterClientException(string message) : Exception(message)
{
}

internal sealed class ProcessTaskActionRequest
{
    public long ProcessInstanceId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? OperatorId { get; set; }
    public string? OperatorName { get; set; }
    public string? Comment { get; set; }
}
