using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using ProcessCenter.API.Contracts;

namespace ProcessCenter.API.Services;

public interface IBusinessCallbackClient
{
    Task<BusinessCallbackResultDto?> NotifyApprovedAsync(
        ProcessInstanceDto instance,
        string? operatorId,
        string? operatorName,
        string? comment,
        CancellationToken cancellationToken = default);

    Task<BusinessCallbackResultDto?> NotifyRejectedAsync(
        ProcessInstanceDto instance,
        string? operatorId,
        string? operatorName,
        string? comment,
        CancellationToken cancellationToken = default);
}

public sealed class BusinessCallbackOptions
{
    public string HrmsBaseUrl { get; set; } = "http://localhost:5000";
}

public sealed class BusinessCallbackHttpClient(
    HttpClient httpClient,
    IOptions<BusinessCallbackOptions> options,
    IOptions<InternalCallbackOptions> callbackOptions) : IBusinessCallbackClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public Task<BusinessCallbackResultDto?> NotifyApprovedAsync(
        ProcessInstanceDto instance,
        string? operatorId,
        string? operatorName,
        string? comment,
        CancellationToken cancellationToken = default)
    {
        return NotifyAsync(instance, isApproved: true, operatorId, operatorName, comment, cancellationToken);
    }

    public Task<BusinessCallbackResultDto?> NotifyRejectedAsync(
        ProcessInstanceDto instance,
        string? operatorId,
        string? operatorName,
        string? comment,
        CancellationToken cancellationToken = default)
    {
        return NotifyAsync(instance, isApproved: false, operatorId, operatorName, comment, cancellationToken);
    }

    private async Task<BusinessCallbackResultDto?> NotifyAsync(
        ProcessInstanceDto instance,
        bool isApproved,
        string? operatorId,
        string? operatorName,
        string? comment,
        CancellationToken cancellationToken)
    {
        // 解析回调配置
        string? callbackUrl = null;
        if (!string.IsNullOrWhiteSpace(instance.CallbackConfig))
        {
            try
            {
                using var configDoc = JsonDocument.Parse(instance.CallbackConfig);
                var root = configDoc.RootElement;
                var propName = isApproved ? "ApprovedUrl" : "RejectedUrl";
                if (root.TryGetProperty(propName, out var urlProp))
                {
                    callbackUrl = urlProp.GetString();
                }
            }
            catch (JsonException)
            {
                // 忽略无效 JSON，回退到硬编码逻辑
            }
        }

        // 如果没有配置，回退到原有的 HRMS 硬编码逻辑（为了兼容性）
        if (string.IsNullOrWhiteSpace(callbackUrl))
        {
            if (!string.Equals(instance.BusinessSystem, "HRMS", StringComparison.OrdinalIgnoreCase)
                || !string.Equals(instance.BusinessType, "PayrollRun", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (!Guid.TryParse(instance.BusinessId, out var runId))
            {
                return new BusinessCallbackResultDto { IsSuccess = false, ErrorMessage = $"PayrollRun 业务主键格式无效：{instance.BusinessId}" };
            }

            var baseUrl = options.Value.HrmsBaseUrl?.Trim();
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                return new BusinessCallbackResultDto { IsSuccess = false, ErrorMessage = "BusinessCallbacks:HrmsBaseUrl 未配置且流程定义未指定回调地址。" };
            }

            var path = isApproved
                ? $"/api/v1/payroll/runs/{runId}/workflow-callback/approved"
                : $"/api/v1/payroll/runs/{runId}/workflow-callback/rejected";
            
            callbackUrl = $"{baseUrl.TrimEnd('/')}{path}";
        }

        Uri finalUri;
        try
        {
            finalUri = new Uri(callbackUrl, UriKind.Absolute);
        }
        catch (UriFormatException)
        {
            // 如果是相对路径，尝试基于 HrmsBaseUrl
            var baseUrl = options.Value.HrmsBaseUrl?.Trim();
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                return new BusinessCallbackResultDto { CallbackUrl = callbackUrl, IsSuccess = false, ErrorMessage = $"无效的回调地址且未配置 BaseUrl：{callbackUrl}" };
            }
            finalUri = new Uri(new Uri(baseUrl), callbackUrl);
        }

        var request = new BusinessProcessCallbackRequest
        {
            ProcessInstanceId = instance.Id.ToString(),
            OperatorId = operatorId,
            OperatorName = operatorName,
            Comment = comment
        };

        var payloadJson = JsonSerializer.Serialize(request, SerializerOptions);
        var result = new BusinessCallbackResultDto
        {
            CallbackUrl = finalUri.ToString(),
            Payload = payloadJson
        };

        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, finalUri)
            {
                Content = JsonContent.Create(request, options: SerializerOptions)
            };
            AddCallbackHeaders(httpRequest, finalUri.PathAndQuery, payloadJson);

            using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
            result.StatusCode = (int)response.StatusCode;
            result.Response = await response.Content.ReadAsStringAsync(cancellationToken);
            result.IsSuccess = response.IsSuccessStatusCode;
            
            if (!result.IsSuccess)
            {
                result.ErrorMessage = $"业务系统响应错误，状态码：{result.StatusCode}";
            }
        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
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

public sealed class BusinessCallbackException(string message) : Exception(message)
{
}

internal sealed class BusinessProcessCallbackRequest
{
    public string ProcessInstanceId { get; set; } = string.Empty;
    public string? OperatorId { get; set; }
    public string? OperatorName { get; set; }
    public string? Comment { get; set; }
}
