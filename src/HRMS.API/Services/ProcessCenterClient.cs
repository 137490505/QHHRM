using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using HRMS.Application.DTOs;
using Microsoft.Extensions.Options;

namespace HRMS.API.Services;

public interface IProcessCenterClient
{
    Task<ProcessStartResultDto> StartPayrollApprovalAsync(PayrollApprovalStartRequest request, CancellationToken cancellationToken = default);
}

public sealed class ProcessCenterOptions
{
    public string BaseUrl { get; set; } = "http://localhost:5099";
    public string PayrollApprovalProcessCode { get; set; } = "PAYROLL_APPROVAL";
}

public sealed class ProcessCenterHttpClient(HttpClient httpClient, IOptions<ProcessCenterOptions> options) : IProcessCenterClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<ProcessStartResultDto> StartPayrollApprovalAsync(
        PayrollApprovalStartRequest startRequest,
        CancellationToken cancellationToken = default)
    {
        var run = startRequest.Run;
        if (httpClient.BaseAddress is null)
        {
            var baseUrl = options.Value.BaseUrl?.Trim();
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                throw new InvalidOperationException("ProcessCenter BaseUrl 未配置。");
            }

            httpClient.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
        }

        var processRequest = new ProcessStartRequest
        {
            ProcessCode = options.Value.PayrollApprovalProcessCode,
            BusinessSystem = "HRMS",
            BusinessType = "PayrollRun",
            BusinessId = run.Id.ToString(),
            Title = $"{run.YearMonth} 薪资审批",
            StarterId = startRequest.StarterId,
            StarterName = startRequest.StarterName,
            RequestId = $"payroll-run:{run.Id}:submit-approval",
            ExtData = JsonSerializer.Serialize(startRequest.SourcePayload, SerializerOptions)
        };

        using var response = await httpClient.PostAsJsonAsync("/api/process/start", processRequest, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new ProcessCenterClientException(
                $"调用 ProcessCenter 启动薪资审批流程失败，状态码：{(int)response.StatusCode}，响应：{responseText}");
        }

        var envelope = await response.Content.ReadFromJsonAsync<ProcessCenterEnvelope<ProcessInstanceData>>(cancellationToken: cancellationToken);
        var data = envelope?.Data ?? throw new ProcessCenterClientException("ProcessCenter 返回内容缺少 data。");
        return new ProcessStartResultDto
        {
            ProcessCode = processRequest.ProcessCode,
            RequestId = processRequest.RequestId,
            ProcessInstanceId = data.Id.ToString(),
            CurrentNodeId = data.CurrentNodeId,
            CurrentNodeName = data.CurrentNodeName,
            CurrentAssigneeId = data.CurrentTodo?.AssigneeId,
            CurrentAssigneeName = data.CurrentTodo?.AssigneeName
        };
    }
}

public sealed class PayrollApprovalStartRequest
{
    public PayrollRunDto Run { get; set; } = new();
    public string StarterId { get; set; } = string.Empty;
    public string StarterName { get; set; } = string.Empty;
    public PayrollApprovalSourcePayload SourcePayload { get; set; } = new();
}

public sealed class PayrollApprovalSourcePayload
{
    public string Scene { get; set; } = "PayrollApproval";
    public Guid RunId { get; set; }
    public string RunNo { get; set; } = string.Empty;
    public string YearMonth { get; set; } = string.Empty;
    public string RunType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int PayrollCount { get; set; }
    public decimal TotalGross { get; set; }
    public decimal TotalNetSalary { get; set; }
    public string? Remark { get; set; }
    public string StarterId { get; set; } = string.Empty;
    public string StarterName { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
}

public sealed class ProcessCenterClientException(string message) : Exception(message)
{
}

public sealed class ProcessStartResultDto
{
    public string ProcessCode { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    public string ProcessInstanceId { get; set; } = string.Empty;
    public string? CurrentNodeId { get; set; }
    public string? CurrentNodeName { get; set; }
    public string? CurrentAssigneeId { get; set; }
    public string? CurrentAssigneeName { get; set; }
}

internal sealed class ProcessStartRequest
{
    public string ProcessCode { get; set; } = string.Empty;
    public string BusinessSystem { get; set; } = string.Empty;
    public string BusinessType { get; set; } = string.Empty;
    public string BusinessId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string StarterId { get; set; } = string.Empty;
    public string StarterName { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    public string? ExtData { get; set; }
}

internal sealed class ProcessCenterEnvelope<T>
{
    public int Code { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public string TraceId { get; set; } = string.Empty;
}

internal sealed class ProcessInstanceData
{
    public long Id { get; set; }
    public string? CurrentNodeId { get; set; }
    public string? CurrentNodeName { get; set; }
    public ProcessTodoData? CurrentTodo { get; set; }
}

internal sealed class ProcessTodoData
{
    [JsonPropertyName("assigneeId")]
    public string? AssigneeId { get; set; }

    [JsonPropertyName("assigneeName")]
    public string? AssigneeName { get; set; }
}
