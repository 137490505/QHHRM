using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TodoCenter.API.Contracts;
using TodoCenter.API.Data;
using TodoCenter.API.Models;
using TodoCenter.API.Services;

var builder = WebApplication.CreateBuilder(args);
var appStartedAt = DateTimeOffset.UtcNow;

if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_URLS")))
{
    builder.WebHost.UseUrls("http://localhost:5163");
}

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<TodoDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

builder.Services.AddScoped<TodoService>();
builder.Services.AddSingleton<ITodoNotificationService, DefaultNotificationService>();
builder.Services.Configure<ProcessCenterOptions>(builder.Configuration.GetSection("ProcessCenter"));
builder.Services.Configure<InternalCallbackOptions>(builder.Configuration.GetSection("InternalCallbacks"));
builder.Services.AddHttpClient<IProcessCenterClient, ProcessCenterHttpClient>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/health", (IHostEnvironment environment) => Results.Ok(new
{
    status = "Healthy",
    service = "TodoCenter.API",
    environment = environment.EnvironmentName,
    startedAt = appStartedAt,
    now = DateTimeOffset.UtcNow
}));

var todoGroup = app.MapGroup("/api/v1/todo");

todoGroup.MapPost("/tasks", async (
    CreateTodoTaskRequest request,
    TodoService service,
    ITodoNotificationService notifyService,
    HttpContext httpContext) =>
{
    var validationResult = ValidationHelper.Validate(request);
    if (validationResult is not null)
    {
        return ApiResults.BadRequest(httpContext, "请求参数校验失败", validationResult);
    }

    var task = await service.CreateTaskAsync(request);
    await notifyService.SendAsync(task, TodoNotifyTypes.NewTask, $"您有一项新任务：{task.Title}");
    return ApiResults.Created(httpContext, task, "任务创建成功");
});

todoGroup.MapGet("/tasks/{id:long}", async (
    long id,
    TodoService service,
    HttpContext httpContext) =>
{
    var task = await service.GetTaskAsync(id);
    return task is null
        ? ApiResults.NotFound(httpContext, "任务不存在")
        : ApiResults.Ok(httpContext, task);
});

todoGroup.MapGet("/tasks/{id:long}/detail", async (
    long id,
    TodoService service,
    HttpContext httpContext) =>
{
    var task = await service.GetTaskDetailAsync(id);
    return task is null
        ? ApiResults.NotFound(httpContext, "任务不存在")
        : ApiResults.Ok(httpContext, task);
});

todoGroup.MapGet("/tasks/my-tasks", async (
    string? assigneeId,
    string? status,
    string? businessType,
    string? taskTypeCode,
    string? businessId,
    string? processInstanceId,
    string? viewType,
    string? createdBy,
    string? taskCategory,
    string? keyword,
    int? priority,
    DateTime? createdStart,
    DateTime? createdEnd,
    DateTime? completedStart,
    DateTime? completedEnd,
    TodoService service,
    HttpContext httpContext) =>
{
    var tasks = await service.QueryTasksAsync(
        assigneeId,
        status,
        businessType,
        taskTypeCode,
        businessId,
        processInstanceId,
        viewType,
        createdBy,
        taskCategory,
        keyword,
        priority,
        createdStart,
        createdEnd,
        completedStart,
        completedEnd);
    return ApiResults.Ok(httpContext, tasks);
});

todoGroup.MapGet("/tasks/view-summary", async (
    string? assigneeId,
    string? createdBy,
    TodoService service,
    HttpContext httpContext) =>
{
    var summary = await service.GetViewSummaryAsync(assigneeId, createdBy);
    return ApiResults.Ok(httpContext, summary);
});

todoGroup.MapPut("/tasks/{id:long}/complete", async (
    long id,
    TodoTaskActionRequest request,
    TodoService service,
    IProcessCenterClient processCenterClient,
    HttpContext httpContext) =>
{
    var task = await service.CompleteTaskAsync(id, request);
    if (task is null)
    {
        return ApiResults.BadRequest(httpContext, "任务不存在或当前状态不可完成");
    }

    if (!string.IsNullOrWhiteSpace(task.ProcessInstanceId))
    {
        try
        {
            await processCenterClient.CompleteTaskAsync(task.ProcessInstanceId, request.OperatorId, request.OperatorName, request.Comment, httpContext.RequestAborted);
        }
        catch (ProcessCenterClientException exception)
        {
            return ApiResults.BadGateway(httpContext, "待办已完成，但流程推进失败", new
            {
                task,
                error = exception.Message
            });
        }
    }

    return ApiResults.Ok(httpContext, task, "任务已完成");
});

todoGroup.MapPut("/tasks/{id:long}/reject", async (
    long id,
    TodoTaskActionRequest request,
    TodoService service,
    IProcessCenterClient processCenterClient,
    HttpContext httpContext) =>
{
    var task = await service.RejectTaskAsync(id, request);
    if (task is null)
    {
        return ApiResults.BadRequest(httpContext, "任务不存在或当前状态不可驳回");
    }

    if (!string.IsNullOrWhiteSpace(task.ProcessInstanceId))
    {
        try
        {
            await processCenterClient.RejectTaskAsync(task.ProcessInstanceId, request.OperatorId, request.OperatorName, request.Comment, httpContext.RequestAborted);
        }
        catch (ProcessCenterClientException exception)
        {
            return ApiResults.BadGateway(httpContext, "待办已驳回，但流程推进失败", new
            {
                task,
                error = exception.Message
            });
        }
    }

    return ApiResults.Ok(httpContext, task, "任务已驳回");
});

todoGroup.MapPut("/tasks/{id:long}/transfer", async (
    long id,
    TodoTaskTransferRequest request,
    TodoService service,
    HttpContext httpContext) =>
{
    var validationResult = ValidationHelper.Validate(request);
    if (validationResult is not null)
    {
        return ApiResults.BadRequest(httpContext, "请求参数校验失败", validationResult);
    }

    var result = await service.TransferTaskAsync(id, request);
    return result is null
        ? ApiResults.BadRequest(httpContext, "任务不存在或当前状态不可转交")
        : ApiResults.Ok(httpContext, result, "任务已转交");
});

todoGroup.MapPut("/tasks/{id:long}/claim", async (
    long id,
    TodoTaskClaimRequest request,
    TodoService service,
    HttpContext httpContext) =>
{
    var validationResult = ValidationHelper.Validate(request);
    if (validationResult is not null)
    {
        return ApiResults.BadRequest(httpContext, "请求参数校验失败", validationResult);
    }

    var result = await service.ClaimTaskAsync(id, request);
    return result is null
        ? ApiResults.BadRequest(httpContext, "任务不存在或当前状态不可认领")
        : ApiResults.Ok(httpContext, result, "任务已认领");
});

todoGroup.MapPut("/tasks/{id:long}/release", async (
    long id,
    TodoTaskActionRequest request,
    TodoService service,
    HttpContext httpContext) =>
{
    var result = await service.ReleaseTaskAsync(id, request);
    return result is null
        ? ApiResults.BadRequest(httpContext, "任务不存在或当前状态不可释放到任务池")
        : ApiResults.Ok(httpContext, result, "任务已释放到任务池");
});

todoGroup.MapPost("/tasks/batch/complete", async (
    TodoBatchCompleteRequest request,
    TodoService service,
    IProcessCenterClient processCenterClient,
    HttpContext httpContext) =>
{
    var validationResult = ValidationHelper.Validate(request);
    if (validationResult is not null)
    {
        return ApiResults.BadRequest(httpContext, "请求参数校验失败", validationResult);
    }

    var result = await service.BatchCompleteAsync(request);
    var completedTasks = result.Tasks
        .Where(task => !string.IsNullOrWhiteSpace(task.ProcessInstanceId))
        .ToArray();

    var errors = new List<object>();
    foreach (var task in completedTasks)
    {
        try
        {
            await processCenterClient.CompleteTaskAsync(task.ProcessInstanceId!, request.OperatorId, request.OperatorName, request.Comment, httpContext.RequestAborted);
        }
        catch (ProcessCenterClientException exception)
        {
            errors.Add(new
            {
                taskId = task.Id,
                processInstanceId = task.ProcessInstanceId,
                error = exception.Message
            });
        }
    }

    if (errors.Count > 0)
    {
        return ApiResults.BadGateway(httpContext, "批量完成已执行，但部分流程推进失败", new
        {
            completedCount = result.CompletedCount,
            tasks = result.Tasks,
            errors
        });
    }

    return ApiResults.Ok(httpContext, result, "批量完成成功");
});

todoGroup.MapPost("/tasks/batch/reject", async (
    TodoBatchRejectRequest request,
    TodoService service,
    IProcessCenterClient processCenterClient,
    HttpContext httpContext) =>
{
    var validationResult = ValidationHelper.Validate(request);
    if (validationResult is not null)
    {
        return ApiResults.BadRequest(httpContext, "请求参数校验失败", validationResult);
    }

    var result = await service.BatchRejectAsync(request);
    var rejectedTasks = result.Tasks
        .Where(task => !string.IsNullOrWhiteSpace(task.ProcessInstanceId))
        .ToArray();

    var errors = new List<object>();
    foreach (var task in rejectedTasks)
    {
        try
        {
            await processCenterClient.RejectTaskAsync(task.ProcessInstanceId!, request.OperatorId, request.OperatorName, request.Comment, httpContext.RequestAborted);
        }
        catch (ProcessCenterClientException exception)
        {
            errors.Add(new
            {
                taskId = task.Id,
                processInstanceId = task.ProcessInstanceId,
                error = exception.Message
            });
        }
    }

    if (errors.Count > 0)
    {
        return ApiResults.BadGateway(httpContext, "批量驳回已执行，但部分流程推进失败", new
        {
            rejectedCount = result.RejectedCount,
            tasks = result.Tasks,
            errors
        });
    }

    return ApiResults.Ok(httpContext, result, "批量驳回成功");
});

todoGroup.MapPost("/tasks/batch/transfer", async (
    TodoBatchTransferRequest request,
    TodoService service,
    HttpContext httpContext) =>
{
    var validationResult = ValidationHelper.Validate(request);
    if (validationResult is not null)
    {
        return ApiResults.BadRequest(httpContext, "请求参数校验失败", validationResult);
    }

    var result = await service.BatchTransferAsync(request);
    return ApiResults.Ok(httpContext, result, "批量转交成功");
});

todoGroup.MapPost("/tasks/batch/urge", async (
    TodoBatchUrgeRequest request,
    TodoService service,
    HttpContext httpContext) =>
{
    var validationResult = ValidationHelper.Validate(request);
    if (validationResult is not null)
    {
        return ApiResults.BadRequest(httpContext, "请求参数校验失败", validationResult);
    }

    var result = await service.BatchUrgeAsync(request);
    return ApiResults.Ok(httpContext, result, "批量催办成功");
});

todoGroup.MapPost("/tasks/cancel/by-process-node", async (
    TodoCancelByProcessNodeRequest request,
    TodoService service,
    HttpContext httpContext) =>
{
    var validationResult = ValidationHelper.Validate(request);
    if (validationResult is not null)
    {
        return ApiResults.BadRequest(httpContext, "请求参数校验失败", validationResult);
    }

    var result = await service.CancelTasksByProcessNodeAsync(request);
    return ApiResults.Ok(httpContext, result, "流程节点关联待办已更新");
});

todoGroup.MapPost("/tasks/{id:long}/urge", async (
    long id,
    TodoTaskActionRequest request,
    TodoService service,
    HttpContext httpContext) =>
{
    var result = await service.UrgeTaskAsync(id, request);
    return result is null
        ? ApiResults.BadRequest(httpContext, "任务不存在，无法催办")
        : ApiResults.Ok(httpContext, result, "催办通知已记录");
});

todoGroup.MapPost("/tasks/{id:long}/copy", async (
    long id,
    TodoTaskCopyRequest request,
    TodoService service,
    HttpContext httpContext) =>
{
    var validationResult = ValidationHelper.Validate(request);
    if (validationResult is not null)
    {
        return ApiResults.BadRequest(httpContext, "请求参数校验失败", validationResult);
    }

    var result = await service.CopyTaskAsync(id, request);
    return result is null
        ? ApiResults.BadRequest(httpContext, "原任务不存在，无法创建抄送任务")
        : ApiResults.Ok(httpContext, result, "抄送任务已创建");
});

todoGroup.MapPut("/tasks/{id:long}/read", async (
    long id,
    TodoTaskActionRequest request,
    TodoService service,
    HttpContext httpContext) =>
{
    var result = await service.MarkCopyTaskReadAsync(id, request);
    return result is null
        ? ApiResults.BadRequest(httpContext, "任务不存在或当前状态不可标记已读")
        : ApiResults.Ok(httpContext, result, "任务已标记已读");
});

todoGroup.MapPost("/agent/settings", async (
    CreateAgentSettingRequest request,
    TodoService service,
    HttpContext httpContext) =>
{
    var validationResult = ValidationHelper.Validate(request);
    if (validationResult is not null)
    {
        return ApiResults.BadRequest(httpContext, "请求参数校验失败", validationResult);
    }

    if (request.EndTime <= request.StartTime)
    {
        return ApiResults.BadRequest(httpContext, "代理结束时间必须晚于开始时间");
    }

    var setting = await service.CreateAgentSettingAsync(request);
    return ApiResults.Created(httpContext, setting, "代理规则创建成功");
});

todoGroup.MapPut("/agent/settings/{id:long}", async (
    long id,
    UpdateAgentSettingRequest request,
    TodoService service,
    HttpContext httpContext) =>
{
    var validationResult = ValidationHelper.Validate(request);
    if (validationResult is not null)
    {
        return ApiResults.BadRequest(httpContext, "请求参数校验失败", validationResult);
    }

    if (request.EndTime <= request.StartTime)
    {
        return ApiResults.BadRequest(httpContext, "代理结束时间必须晚于开始时间");
    }

    var setting = await service.UpdateAgentSettingAsync(id, request);
    return setting is null
        ? ApiResults.NotFound(httpContext, "代理规则不存在")
        : ApiResults.Ok(httpContext, setting, "代理规则已更新");
});

todoGroup.MapPut("/agent/settings/{id:long}/disable", async (
    long id,
    TodoService service,
    HttpContext httpContext) =>
{
    var setting = await service.DisableAgentSettingAsync(id);
    return setting is null
        ? ApiResults.NotFound(httpContext, "代理规则不存在")
        : ApiResults.Ok(httpContext, setting, "代理规则已停用");
});

todoGroup.MapDelete("/agent/settings/{id:long}", async (
    long id,
    TodoService service,
    HttpContext httpContext) =>
{
    var deleted = await service.DeleteAgentSettingAsync(id);
    return !deleted
        ? ApiResults.NotFound(httpContext, "代理规则不存在")
        : ApiResults.Ok(httpContext, new { id }, "代理规则已删除");
});

todoGroup.MapGet("/agent/settings", async (
    string? principalUserId,
    TodoService service,
    HttpContext httpContext) =>
{
    var settings = await service.QueryAgentSettingsAsync(principalUserId);
    return ApiResults.Ok(httpContext, settings);
});

todoGroup.MapGet("/kpi/summary", async (
    string? assigneeId,
    TodoService service,
    HttpContext httpContext) =>
{
    var summary = await service.GetKpiSummaryAsync(assigneeId);
    return ApiResults.Ok(httpContext, summary);
});

todoGroup.MapGet("/kpi/analysis", async (
    string? assigneeId,
    string? dimension,
    TodoService service,
    HttpContext httpContext) =>
{
    var analysis = await service.GetKpiAnalysisAsync(assigneeId, dimension);
    return ApiResults.Ok(httpContext, analysis);
});

todoGroup.MapGet("/pool/summary", async (
    string? userId,
    TodoService service,
    HttpContext httpContext) =>
{
    var summary = await service.GetPoolSummaryAsync(userId);
    return ApiResults.Ok(httpContext, summary);
});

todoGroup.MapPost("/tasks/scan-timeouts", async (
    TodoService service,
    ITodoNotificationService notifyService,
    HttpContext httpContext) =>
{
    var timeoutTasks = await service.ScanTimeoutsAsync();
    foreach (var task in timeoutTasks)
    {
        await notifyService.SendAsync(task, TodoNotifyTypes.Timeout, $"您的任务 [{task.Title}] 已超时，请尽快处理。");
    }
    return ApiResults.Ok(httpContext, new { scannedCount = timeoutTasks.Count }, "超时扫描完成");
});

app.Run();

static class ValidationHelper
{
    public static Dictionary<string, string[]>? Validate<T>(T model)
    {
        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(model!);
        var isValid = Validator.TryValidateObject(
            model!,
            context,
            validationResults,
            validateAllProperties: true);

        if (isValid)
        {
            return null;
        }

        return validationResults
            .GroupBy(
                result => result.MemberNames.FirstOrDefault() ?? string.Empty,
                StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Select(result => result.ErrorMessage ?? "参数无效").ToArray(),
                StringComparer.OrdinalIgnoreCase);
    }
}

static class ApiResults
{
    public static IResult Ok(HttpContext context, object? data, string message = "success")
    {
        return Results.Json(ApiEnvelope.Success(context.TraceIdentifier, data, message));
    }

    public static IResult Created(HttpContext context, object? data, string message)
    {
        return Results.Json(ApiEnvelope.Success(context.TraceIdentifier, data, message), statusCode: StatusCodes.Status201Created);
    }

    public static IResult BadRequest(HttpContext context, string message, object? data = null)
    {
        return Results.Json(ApiEnvelope.Failure(context.TraceIdentifier, 400, message, data), statusCode: StatusCodes.Status400BadRequest);
    }

    public static IResult BadGateway(HttpContext context, string message, object? data = null)
    {
        return Results.Json(ApiEnvelope.Failure(context.TraceIdentifier, 502, message, data), statusCode: StatusCodes.Status502BadGateway);
    }

    public static IResult NotFound(HttpContext context, string message)
    {
        return Results.Json(ApiEnvelope.Failure(context.TraceIdentifier, 404, message), statusCode: StatusCodes.Status404NotFound);
    }
}

sealed record ApiEnvelope(int Code, string Message, object? Data, string TraceId)
{
    public static ApiEnvelope Success(string traceId, object? data, string message)
        => new(200, message, data, traceId);

    public static ApiEnvelope Failure(string traceId, int code, string message, object? data = null)
        => new(code, message, data, traceId);
}

public class TodoService
{
    private readonly TodoDbContext _dbContext;

    public TodoService(TodoDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TodoTaskDto> CreateTaskAsync(CreateTodoTaskRequest request)
    {
        var resolvedAssignee = await ResolveAssigneeAsync(request.AssigneeId, request.AssigneeName, request.TaskTypeCode);
        var existingTask = await FindExistingTaskAsync(request, resolvedAssignee.AssigneeId);
        if (existingTask is not null)
        {
            return MapTask(existingTask);
        }

        var now = DateTime.UtcNow;
        var task = new TodoTaskEntity
        {
            TaskNo = $"TD{DateTime.UtcNow:yyyyMMddHHmmss}{Guid.NewGuid().ToString("N")[..6].ToUpper()}",
            TaskTypeCode = request.TaskTypeCode,
            Title = request.Title,
            BusinessSystem = request.BusinessSystem,
            BusinessType = request.BusinessType,
            BusinessId = request.BusinessId,
            ProcessInstanceId = request.ProcessInstanceId,
            ProcessNodeId = request.ProcessNodeId,
            AssigneeId = resolvedAssignee.AssigneeId,
            AssigneeName = resolvedAssignee.AssigneeName,
            OwnerDeptId = request.OwnerDeptId,
            Priority = request.Priority,
            DueTime = request.DueTime,
            SourcePayload = request.SourcePayload,
            ExtData = MergeExtData(request.ExtData, resolvedAssignee.AgentNote),
            CreatedBy = request.CreatedBy,
            UpdatedBy = request.CreatedBy,
            CreatedTime = now,
            UpdatedTime = now
        };

        _dbContext.TodoTasks.Add(task);
        await AddTaskLogAsync(0, "Create", request.CreatedBy, request.CreatedBy, "Created", null, task.Status, resolvedAssignee.AgentNote, task);
        await _dbContext.SaveChangesAsync();

        return MapTask(task);
    }

    public async Task<TodoTaskDto?> GetTaskAsync(long id)
    {
        var task = await _dbContext.TodoTasks.FindAsync(id);
        return task is not null ? MapTask(task) : null;
    }

    public async Task<TodoTaskDetailDto?> GetTaskDetailAsync(long id)
    {
        var task = await _dbContext.TodoTasks.FindAsync(id);
        if (task is null) return null;

        var logs = await _dbContext.TodoTaskLogs
            .Where(l => l.TaskId == id)
            .OrderByDescending(l => l.Id)
            .ToListAsync();

        var notifyLogs = await _dbContext.TodoTaskNotifyLogs
            .Where(l => l.TaskId == id)
            .OrderByDescending(l => l.Id)
            .ToListAsync();

        return new TodoTaskDetailDto
        {
            Task = MapTask(task),
            Logs = logs.Select(MapTaskLog).ToList(),
            NotifyLogs = notifyLogs.Select(MapNotifyLog).ToList()
        };
    }

    public async Task<IReadOnlyList<TodoTaskDto>> QueryTasksAsync(
        string? assigneeId,
        string? status,
        string? businessType,
        string? taskTypeCode,
        string? businessId,
        string? processInstanceId,
        string? viewType,
        string? createdBy,
        string? taskCategory,
        string? keyword,
        int? priority,
        DateTime? createdStart,
        DateTime? createdEnd,
        DateTime? completedStart,
        DateTime? completedEnd)
    {
        var query = _dbContext.TodoTasks.AsQueryable();

        // 视图类型过滤
        if (!string.IsNullOrWhiteSpace(viewType))
        {
            switch (viewType.ToLowerInvariant())
            {
                case "pending":
                    query = query.Where(t => t.Status == "Pending" || t.Status == "InProgress");
                    break;
                case "processed":
                    query = query.Where(t => t.AssigneeId == assigneeId && (t.Status == "Completed" || t.Status == "Rejected" || t.Status == "Transferred"));
                    break;
                case "created":
                    query = query.Where(t => t.CreatedBy == createdBy);
                    break;
                case "copied":
                    query = query.Where(t => t.TaskCategory == "Copy");
                    break;
                case "pool":
                    query = query.Where(t => (t.Status == "Pending" || t.Status == "InProgress") && (t.AssigneeId == null || t.AssigneeId == ""));
                    break;
                case "agent":
                    query = query.Where(t => t.ExtData != null && t.ExtData.Contains("delegatedByRuleId"));
                    break;
            }
        }

        if (!string.IsNullOrWhiteSpace(assigneeId)) query = query.Where(t => t.AssigneeId == assigneeId);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(t => t.Status == status);
        if (!string.IsNullOrWhiteSpace(businessType)) query = query.Where(t => t.BusinessType == businessType);
        if (!string.IsNullOrWhiteSpace(taskTypeCode)) query = query.Where(t => t.TaskTypeCode == taskTypeCode);
        if (!string.IsNullOrWhiteSpace(businessId)) query = query.Where(t => t.BusinessId == businessId);
        if (!string.IsNullOrWhiteSpace(processInstanceId)) query = query.Where(t => t.ProcessInstanceId == processInstanceId);
        if (!string.IsNullOrWhiteSpace(taskCategory)) query = query.Where(t => t.TaskCategory == taskCategory);
        if (priority.HasValue) query = query.Where(t => t.Priority == priority.Value);
        if (createdStart.HasValue) query = query.Where(t => t.CreatedTime >= createdStart.Value);
        if (createdEnd.HasValue) query = query.Where(t => t.CreatedTime <= createdEnd.Value);
        if (completedStart.HasValue) query = query.Where(t => t.CompletedTime >= completedStart.Value);
        if (completedEnd.HasValue) query = query.Where(t => t.CompletedTime <= completedEnd.Value);
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(t => t.Title.Contains(keyword) || t.TaskNo.Contains(keyword) || t.BusinessId.Contains(keyword));
        }

        var results = await query
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.DueTime ?? DateTime.MaxValue)
            .ThenByDescending(t => t.CreatedTime)
            .ToListAsync();

        return results.Select(MapTask).ToList();
    }

    public async Task<TodoTaskViewSummaryDto> GetViewSummaryAsync(string? assigneeId, string? createdBy)
    {
        var now = DateTime.UtcNow;
        return new TodoTaskViewSummaryDto
        {
            PendingCount = await _dbContext.TodoTasks.CountAsync(t => (t.Status == "Pending" || t.Status == "InProgress") && (string.IsNullOrEmpty(assigneeId) || t.AssigneeId == assigneeId)),
            ProcessedCount = await _dbContext.TodoTasks.CountAsync(t => t.AssigneeId == assigneeId && (t.Status == "Completed" || t.Status == "Rejected" || t.Status == "Transferred")),
            CreatedCount = await _dbContext.TodoTasks.CountAsync(t => t.CreatedBy == createdBy),
            CopiedCount = await _dbContext.TodoTasks.CountAsync(t => t.TaskCategory == "Copy" && (string.IsNullOrEmpty(assigneeId) || t.AssigneeId == assigneeId)),
            PoolCount = await _dbContext.TodoTasks.CountAsync(t => (t.Status == "Pending" || t.Status == "InProgress") && (t.AssigneeId == null || t.AssigneeId == "")),
            TransferredCount = await _dbContext.TodoTaskLogs.CountAsync(l => l.OperatorId == assigneeId && (l.Action == "TransferOut" || l.Action == "BatchTransferOut")),
            AgentPendingCount = await _dbContext.TodoTasks.CountAsync(t => (t.Status == "Pending" || t.Status == "InProgress") && t.ExtData != null && t.ExtData.Contains("delegatedByRuleId") && (string.IsNullOrEmpty(assigneeId) || t.AssigneeId == assigneeId))
        };
    }

    public async Task<TodoTaskDto?> CompleteTaskAsync(long id, TodoTaskActionRequest request)
    {
        var task = await _dbContext.TodoTasks.FindAsync(id);
        if (!CanOperate(task)) return null;

        var beforeStatus = task!.Status;
        var now = DateTime.UtcNow;
        task.Status = "Completed";
        task.Result = "Completed";
        task.CompletedTime = now;
        task.UpdatedBy = request.OperatorId ?? request.OperatorName;
        task.UpdatedTime = now;

        await AddTaskLogAsync(task.Id, "Complete", request.OperatorId, request.OperatorName, task.Result, beforeStatus, task.Status, request.Comment);
        await _dbContext.SaveChangesAsync();
        return MapTask(task);
    }

    public async Task<TodoTaskDto?> RejectTaskAsync(long id, TodoTaskActionRequest request)
    {
        var task = await _dbContext.TodoTasks.FindAsync(id);
        if (!CanOperate(task)) return null;

        var beforeStatus = task!.Status;
        var now = DateTime.UtcNow;
        task.Status = "Rejected";
        task.Result = "Rejected";
        task.CompletedTime = now;
        task.UpdatedBy = request.OperatorId ?? request.OperatorName;
        task.UpdatedTime = now;

        await AddTaskLogAsync(task.Id, "Reject", request.OperatorId, request.OperatorName, task.Result, beforeStatus, task.Status, request.Comment);
        await _dbContext.SaveChangesAsync();
        return MapTask(task);
    }

    public async Task<object?> TransferTaskAsync(long id, TodoTaskTransferRequest request)
    {
        var task = await _dbContext.TodoTasks.FindAsync(id);
        if (!CanOperate(task)) return null;

        var now = DateTime.UtcNow;
        var beforeStatus = task!.Status;
        task.Status = "Transferred";
        task.Result = "Transferred";
        task.CompletedTime = now;
        task.UpdatedBy = request.OperatorId ?? request.OperatorName;
        task.UpdatedTime = now;

        await AddTaskLogAsync(task.Id, "TransferOut", request.OperatorId, request.OperatorName, task.Result, beforeStatus, task.Status, request.Comment);

        var newTask = new TodoTaskEntity
        {
            TaskNo = $"TD{DateTime.UtcNow:yyyyMMddHHmmss}{Guid.NewGuid().ToString("N")[..6].ToUpper()}",
            TaskTypeCode = task.TaskTypeCode,
            Title = task.Title,
            BusinessSystem = task.BusinessSystem,
            BusinessType = task.BusinessType,
            BusinessId = task.BusinessId,
            ProcessInstanceId = task.ProcessInstanceId,
            ProcessNodeId = task.ProcessNodeId,
            AssigneeId = request.TargetAssigneeId,
            AssigneeName = request.TargetAssigneeName,
            OwnerDeptId = task.OwnerDeptId,
            Priority = task.Priority,
            DueTime = task.DueTime,
            SourcePayload = task.SourcePayload,
            ExtData = MergeExtData(task.ExtData, $"{{\"transferredFromTaskId\":{task.Id}}}"),
            CreatedBy = request.OperatorId ?? request.OperatorName,
            UpdatedBy = request.OperatorId ?? request.OperatorName,
            CreatedTime = now,
            UpdatedTime = now
        };

        _dbContext.TodoTasks.Add(newTask);
        await AddTaskLogAsync(0, "TransferIn", request.OperatorId, request.OperatorName, "Pending", null, newTask.Status, request.Comment, newTask);
        await _dbContext.SaveChangesAsync();

        return new
        {
            originalTask = MapTask(task),
            newTask = MapTask(newTask)
        };
    }

    public async Task<TodoTaskDto?> ClaimTaskAsync(long id, TodoTaskClaimRequest request)
    {
        var task = await _dbContext.TodoTasks.FindAsync(id);
        if (!CanClaim(task)) return null;

        var now = DateTime.UtcNow;
        task!.AssigneeId = request.AssigneeId;
        task.AssigneeName = request.AssigneeName;
        task.UpdatedBy = request.OperatorId ?? request.OperatorName ?? request.AssigneeId;
        task.UpdatedTime = now;

        await AddTaskLogAsync(task.Id, "Claim", request.OperatorId ?? request.AssigneeId, request.OperatorName ?? request.AssigneeName, "Claimed", task.Status, task.Status, request.Comment);
        await _dbContext.SaveChangesAsync();
        return MapTask(task);
    }

    public async Task<TodoTaskDto?> ReleaseTaskAsync(long id, TodoTaskActionRequest request)
    {
        var task = await _dbContext.TodoTasks.FindAsync(id);
        if (!CanOperate(task) || string.Equals(task!.TaskCategory, "Copy", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var now = DateTime.UtcNow;
        task.AssigneeId = string.Empty;
        task.AssigneeName = string.Empty;
        task.UpdatedBy = request.OperatorId ?? request.OperatorName;
        task.UpdatedTime = now;
        task.ExtData = MergeExtData(task.ExtData, JsonSerializer.Serialize(new
        {
            releasedToPool = true,
            releasedAt = now,
            releasedBy = request.OperatorId ?? request.OperatorName
        }));

        await AddTaskLogAsync(task.Id, "ReleaseToPool", request.OperatorId, request.OperatorName, "Released", task.Status, task.Status, request.Comment);
        await _dbContext.SaveChangesAsync();
        return MapTask(task);
    }

    public async Task<BatchCompleteResult> BatchCompleteAsync(TodoBatchCompleteRequest request)
    {
        var completedTasks = new List<TodoTaskDto>();
        var taskIds = request.TaskIds.Distinct().ToList();
        var tasks = await _dbContext.TodoTasks.Where(t => taskIds.Contains(t.Id)).ToListAsync();

        foreach (var task in tasks)
        {
            if (!CanOperate(task)) continue;

            var beforeStatus = task.Status;
            var now = DateTime.UtcNow;
            task.Status = "Completed";
            task.Result = "Completed";
            task.CompletedTime = now;
            task.UpdatedBy = request.OperatorId ?? request.OperatorName;
            task.UpdatedTime = now;

            await AddTaskLogAsync(task.Id, "BatchComplete", request.OperatorId, request.OperatorName, task.Result, beforeStatus, task.Status, request.Comment);
            completedTasks.Add(MapTask(task));
        }

        await _dbContext.SaveChangesAsync();
        return new BatchCompleteResult { CompletedCount = completedTasks.Count, Tasks = completedTasks };
    }

    public async Task<BatchRejectResult> BatchRejectAsync(TodoBatchRejectRequest request)
    {
        var rejectedTasks = new List<TodoTaskDto>();
        var taskIds = request.TaskIds.Distinct().ToList();
        var tasks = await _dbContext.TodoTasks.Where(t => taskIds.Contains(t.Id)).ToListAsync();

        foreach (var task in tasks)
        {
            if (!CanOperate(task)) continue;

            var beforeStatus = task.Status;
            var now = DateTime.UtcNow;
            task.Status = "Rejected";
            task.Result = "Rejected";
            task.CompletedTime = now;
            task.UpdatedBy = request.OperatorId ?? request.OperatorName;
            task.UpdatedTime = now;

            await AddTaskLogAsync(task.Id, "BatchReject", request.OperatorId, request.OperatorName, task.Result, beforeStatus, task.Status, request.Comment);
            rejectedTasks.Add(MapTask(task));
        }

        await _dbContext.SaveChangesAsync();
        return new BatchRejectResult { RejectedCount = rejectedTasks.Count, Tasks = rejectedTasks };
    }

    public async Task<BatchTransferResult> BatchTransferAsync(TodoBatchTransferRequest request)
    {
        var originalTasks = new List<TodoTaskDto>();
        var newTasks = new List<TodoTaskDto>();
        var taskIds = request.TaskIds.Distinct().ToList();
        var tasks = await _dbContext.TodoTasks.Where(t => taskIds.Contains(t.Id)).ToListAsync();

        foreach (var task in tasks)
        {
            if (!CanOperate(task) || string.Equals(task.TaskCategory, "Copy", StringComparison.OrdinalIgnoreCase)) continue;

            var now = DateTime.UtcNow;
            var beforeStatus = task.Status;
            task.Status = "Transferred";
            task.Result = "Transferred";
            task.CompletedTime = now;
            task.UpdatedBy = request.OperatorId ?? request.OperatorName;
            task.UpdatedTime = now;

            await AddTaskLogAsync(task.Id, "BatchTransferOut", request.OperatorId, request.OperatorName, task.Result, beforeStatus, task.Status, request.Comment);

            var newTask = new TodoTaskEntity
            {
                TaskNo = $"TD{DateTime.UtcNow:yyyyMMddHHmmss}{Guid.NewGuid().ToString("N")[..6].ToUpper()}",
                TaskTypeCode = task.TaskTypeCode,
                Title = task.Title,
                BusinessSystem = task.BusinessSystem,
                BusinessType = task.BusinessType,
                BusinessId = task.BusinessId,
                ProcessInstanceId = task.ProcessInstanceId,
                ProcessNodeId = task.ProcessNodeId,
                AssigneeId = request.TargetAssigneeId,
                AssigneeName = request.TargetAssigneeName,
                TaskCategory = task.TaskCategory,
                OwnerDeptId = task.OwnerDeptId,
                Priority = task.Priority,
                DueTime = task.DueTime,
                SourcePayload = task.SourcePayload,
                OriginalTaskId = task.OriginalTaskId ?? task.Id,
                ExtData = MergeExtData(task.ExtData, $"{{\"transferredFromTaskId\":{task.Id}}}"),
                CreatedBy = request.OperatorId ?? request.OperatorName,
                UpdatedBy = request.OperatorId ?? request.OperatorName,
                CreatedTime = now,
                UpdatedTime = now
            };

            _dbContext.TodoTasks.Add(newTask);
            await AddTaskLogAsync(0, "BatchTransferIn", request.OperatorId, request.OperatorName, "Pending", null, newTask.Status, request.Comment, newTask);
            
            originalTasks.Add(MapTask(task));
            newTasks.Add(MapTask(newTask));
        }

        await _dbContext.SaveChangesAsync();

        return new BatchTransferResult { TransferCount = newTasks.Count, OriginalTasks = originalTasks, NewTasks = newTasks };
    }

    public async Task<BatchUrgeResult> BatchUrgeAsync(TodoBatchUrgeRequest request)
    {
        var results = new List<TodoTaskNotifyLogDto>();
        var taskIds = request.TaskIds.Distinct().ToList();
        var tasks = await _dbContext.TodoTasks.Where(t => taskIds.Contains(t.Id)).ToListAsync();

        foreach (var task in tasks)
        {
            var notifyLog = new TodoTaskNotifyLogEntity
            {
                TaskId = task.Id,
                NotifyType = "Urge",
                ReceiverId = task.AssigneeId,
                ReceiverName = string.IsNullOrWhiteSpace(task.AssigneeName) ? "任务池" : task.AssigneeName,
                Channel = "InApp",
                Status = "Sent",
                Content = $"{task.Title} 已被批量催办，请尽快处理。",
                SentTime = DateTime.UtcNow,
                CreatedTime = DateTime.UtcNow
            };

            _dbContext.TodoTaskNotifyLogs.Add(notifyLog);
            await AddTaskLogAsync(task.Id, "BatchUrge", request.OperatorId, request.OperatorName, notifyLog.Status, task.Status, task.Status, request.Comment);
            results.Add(MapNotifyLog(notifyLog));
        }

        await _dbContext.SaveChangesAsync();
        return new BatchUrgeResult { UrgeCount = results.Count, NotifyLogs = results };
    }

    public async Task<CancelByProcessNodeResult> CancelTasksByProcessNodeAsync(TodoCancelByProcessNodeRequest request)
    {
        var canceledTasks = new List<TodoTaskDto>();
        var status = string.IsNullOrWhiteSpace(request.TargetStatus) ? "Canceled" : request.TargetStatus.Trim();

        var tasksToCancel = await _dbContext.TodoTasks
            .Where(t => t.ProcessInstanceId == request.ProcessInstanceId && t.ProcessNodeId == request.ProcessNodeId && t.Status == "Pending" && t.TaskCategory != "Copy")
            .ToListAsync();

        foreach (var task in tasksToCancel)
        {
            var beforeStatus = task.Status;
            task.Status = status;
            task.Result = status;
            task.CompletedTime = DateTime.UtcNow;
            task.UpdatedBy = request.OperatorId ?? request.OperatorName;
            task.UpdatedTime = DateTime.UtcNow;

            await AddTaskLogAsync(task.Id, "CancelByProcessNode", request.OperatorId, request.OperatorName, status, beforeStatus, task.Status, request.Comment);
            canceledTasks.Add(MapTask(task));
        }

        await _dbContext.SaveChangesAsync();
        return new CancelByProcessNodeResult { AffectedCount = canceledTasks.Count, Tasks = canceledTasks };
    }

    public async Task<object?> UrgeTaskAsync(long id, TodoTaskActionRequest request)
    {
        var task = await _dbContext.TodoTasks.FindAsync(id);
        if (task is null) return null;

        var notifyLog = new TodoTaskNotifyLogEntity
        {
            TaskId = task.Id,
            NotifyType = "Urge",
            ReceiverId = task.AssigneeId,
            ReceiverName = task.AssigneeName,
            Channel = "InApp",
            Status = "Sent",
            Content = $"{task.Title} 已被催办，请尽快处理。",
            SentTime = DateTime.UtcNow,
            CreatedTime = DateTime.UtcNow
        };

        _dbContext.TodoTaskNotifyLogs.Add(notifyLog);
        await AddTaskLogAsync(task.Id, "Urge", request.OperatorId, request.OperatorName, notifyLog.Status, task.Status, task.Status, request.Comment);
        await _dbContext.SaveChangesAsync();

        return new { taskId = task.Id, notifyType = notifyLog.NotifyType, receiverId = notifyLog.ReceiverId, receiverName = notifyLog.ReceiverName, sentTime = notifyLog.SentTime };
    }

    public async Task<TodoTaskCopyBatchResultDto?> CopyTaskAsync(long id, TodoTaskCopyRequest request)
    {
        var sourceTask = await _dbContext.TodoTasks.FindAsync(id);
        if (sourceTask is null) return null;

        var now = DateTime.UtcNow;
        var createdTasks = new List<TodoTaskDto>();
        foreach (var receiver in request.Receivers.GroupBy(r => r.ReceiverId).Select(g => g.First()))
        {
            var task = new TodoTaskEntity
            {
                TaskNo = $"TD{DateTime.UtcNow:yyyyMMddHHmmss}{Guid.NewGuid().ToString("N")[..6].ToUpper()}",
                TaskTypeCode = sourceTask.TaskTypeCode,
                Title = $"[抄送] {sourceTask.Title}",
                BusinessSystem = sourceTask.BusinessSystem,
                BusinessType = sourceTask.BusinessType,
                BusinessId = sourceTask.BusinessId,
                ProcessInstanceId = sourceTask.ProcessInstanceId,
                ProcessNodeId = sourceTask.ProcessNodeId,
                AssigneeId = receiver.ReceiverId,
                AssigneeName = receiver.ReceiverName,
                TaskCategory = "Copy",
                Status = "Unread",
                Priority = sourceTask.Priority,
                DueTime = sourceTask.DueTime,
                OriginalTaskId = sourceTask.Id,
                SourcePayload = sourceTask.SourcePayload,
                ExtData = MergeExtData(sourceTask.ExtData, JsonSerializer.Serialize(new { copiedFromTaskId = sourceTask.Id, copiedBy = request.OperatorId ?? request.OperatorName })),
                CreatedBy = request.OperatorId ?? request.OperatorName,
                UpdatedBy = request.OperatorId ?? request.OperatorName,
                CreatedTime = now,
                UpdatedTime = now
            };

            _dbContext.TodoTasks.Add(task);
            await AddTaskLogAsync(0, "CopyCreate", request.OperatorId, request.OperatorName, "Unread", null, task.Status, request.Comment, task);
            createdTasks.Add(MapTask(task));
        }

        await AddTaskLogAsync(sourceTask.Id, "CopyOut", request.OperatorId, request.OperatorName, "Copied", sourceTask.Status, sourceTask.Status, request.Comment);
        await _dbContext.SaveChangesAsync();

        return new TodoTaskCopyBatchResultDto { CreatedCount = createdTasks.Count, Tasks = createdTasks };
    }

    public async Task<TodoTaskDto?> MarkCopyTaskReadAsync(long id, TodoTaskActionRequest request)
    {
        var task = await _dbContext.TodoTasks.FindAsync(id);
        if (task is null || task.TaskCategory != "Copy") return null;

        if (task.Status == "Read") return MapTask(task);
        if (task.Status != "Unread") return null;

        var now = DateTime.UtcNow;
        task.Status = "Read";
        task.Result = "Read";
        task.ReadTime = now;
        task.UpdatedBy = request.OperatorId ?? request.OperatorName;
        task.UpdatedTime = now;

        await AddTaskLogAsync(task.Id, "Read", request.OperatorId, request.OperatorName, "Read", "Unread", task.Status, request.Comment);
        await _dbContext.SaveChangesAsync();
        return MapTask(task);
    }

    public async Task<AgentSettingDto> CreateAgentSettingAsync(CreateAgentSettingRequest request)
    {
        var now = DateTime.UtcNow;
        var setting = new AgentSettingEntity
        {
            PrincipalUserId = request.PrincipalUserId,
            AgentUserId = request.AgentUserId,
            ScopeType = request.ScopeType,
            TaskTypeCode = request.TaskTypeCode,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Status = request.EndTime <= now ? "Expired" : "Active",
            CreatedTime = now,
            UpdatedTime = now
        };

        _dbContext.AgentSettings.Add(setting);
        await _dbContext.SaveChangesAsync();
        return MapAgentSetting(setting);
    }

    public async Task<AgentSettingDto?> UpdateAgentSettingAsync(long id, UpdateAgentSettingRequest request)
    {
        var setting = await _dbContext.AgentSettings.FindAsync(id);
        if (setting is null) return null;

        setting.AgentUserId = request.AgentUserId;
        setting.ScopeType = request.ScopeType;
        setting.TaskTypeCode = request.TaskTypeCode;
        setting.StartTime = request.StartTime;
        setting.EndTime = request.EndTime;
        setting.Status = request.EndTime <= DateTime.UtcNow ? "Expired" : "Active";
        setting.UpdatedTime = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
        return MapAgentSetting(setting);
    }

    public async Task<AgentSettingDto?> DisableAgentSettingAsync(long id)
    {
        var setting = await _dbContext.AgentSettings.FindAsync(id);
        if (setting is null) return null;

        setting.Status = "Disabled";
        setting.EndTime = DateTime.UtcNow;
        setting.UpdatedTime = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
        return MapAgentSetting(setting);
    }

    public async Task<bool> DeleteAgentSettingAsync(long id)
    {
        var setting = await _dbContext.AgentSettings.FindAsync(id);
        if (setting is null) return false;

        _dbContext.AgentSettings.Remove(setting);
        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<IReadOnlyList<AgentSettingDto>> QueryAgentSettingsAsync(string? principalUserId)
    {
        var query = _dbContext.AgentSettings.AsQueryable();
        if (!string.IsNullOrWhiteSpace(principalUserId))
        {
            query = query.Where(s => s.PrincipalUserId == principalUserId);
        }

        var results = await query.OrderByDescending(s => s.UpdatedTime).ToListAsync();
        return results.Select(MapAgentSetting).ToList();
    }

    public async Task<TodoKpiSummaryDto> GetKpiSummaryAsync(string? assigneeId)
    {
        var query = _dbContext.TodoTasks.AsQueryable();
        if (!string.IsNullOrWhiteSpace(assigneeId)) query = query.Where(t => t.AssigneeId == assigneeId);

        var tasks = await query.ToListAsync();
        var completedTasks = tasks.Where(t => t.Status == "Completed").ToList();
        var onTimeCompletedCount = completedTasks.Count(t => t.CompletedTime <= (t.DueTime ?? DateTime.MaxValue));
        var timeoutCount = tasks.Count(t => t.Status == "Pending" && t.DueTime < DateTime.UtcNow);

        return new TodoKpiSummaryDto
        {
            TotalCount = tasks.Count,
            PendingCount = tasks.Count(t => t.Status == "Pending"),
            CompletedCount = completedTasks.Count,
            TimeoutCount = timeoutCount,
            OnTimeRate = completedTasks.Count == 0 ? 1m : Math.Round((decimal)onTimeCompletedCount / completedTasks.Count, 4)
        };
    }

    public async Task<TodoKpiAnalysisDto> GetKpiAnalysisAsync(string? assigneeId, string? dimension)
    {
        var actualDimension = string.IsNullOrWhiteSpace(dimension) ? "taskTypeCode" : dimension.Trim();
        var query = _dbContext.TodoTasks.AsQueryable();
        if (!string.IsNullOrWhiteSpace(assigneeId)) query = query.Where(t => t.AssigneeId == assigneeId);

        var tasks = await query.ToListAsync();

        Func<TodoTaskEntity, string> keySelector = actualDimension.ToLowerInvariant() switch
        {
            "businesstype" => t => t.BusinessType,
            "assignee" => t => string.IsNullOrEmpty(t.AssigneeId) ? "UNASSIGNED" : t.AssigneeId,
            "status" => t => t.Status,
            _ => t => t.TaskTypeCode
        };

        Func<TodoTaskEntity, string> labelSelector = actualDimension.ToLowerInvariant() switch
        {
            "assignee" => t => string.IsNullOrEmpty(t.AssigneeName) ? "任务池" : t.AssigneeName,
            _ => t => keySelector(t)
        };

        var items = tasks.GroupBy(keySelector)
            .Select(g =>
            {
                var groupTasks = g.ToList();
                var completed = groupTasks.Where(t => t.Status == "Completed" || t.Status == "Rejected").ToList();
                var onTime = completed.Count(t => t.CompletedTime <= (t.DueTime ?? DateTime.MaxValue));
                return new TodoKpiDimensionItemDto
                {
                    Key = g.Key,
                    Label = labelSelector(groupTasks.First()),
                    TotalCount = groupTasks.Count,
                    PendingCount = groupTasks.Count(t => t.Status == "Pending"),
                    CompletedCount = groupTasks.Count(t => t.Status == "Completed"),
                    RejectedCount = groupTasks.Count(t => t.Status == "Rejected"),
                    TimeoutCount = groupTasks.Count(t => t.Status == "Pending" && t.DueTime < DateTime.UtcNow),
                    AverageHandleMinutes = completed.Count == 0 ? 0m : (decimal)completed.Average(t => (t.CompletedTime!.Value - t.CreatedTime).TotalMinutes),
                    OnTimeRate = completed.Count == 0 ? 1m : Math.Round((decimal)onTime / completed.Count, 4)
                };
            }).ToList();

        return new TodoKpiAnalysisDto { Dimension = actualDimension, Items = items };
    }

    public async Task<TodoTaskPoolSummaryDto> GetPoolSummaryAsync(string? userId)
    {
        return new TodoTaskPoolSummaryDto
        {
            ClaimableCount = await _dbContext.TodoTasks.CountAsync(t => t.Status == "Pending" && (t.AssigneeId == null || t.AssigneeId == "") && t.TaskCategory != "Copy"),
            CopiedUnreadCount = await _dbContext.TodoTasks.CountAsync(t => t.TaskCategory == "Copy" && t.Status == "Unread" && (string.IsNullOrEmpty(userId) || t.AssigneeId == userId)),
            CreatedByMeCount = await _dbContext.TodoTasks.CountAsync(t => t.CreatedBy == userId),
            ProcessedByMeCount = await _dbContext.TodoTaskLogs.CountAsync(l => l.OperatorId == userId && (l.Action == "Complete" || l.Action == "Reject" || l.Action == "BatchComplete" || l.Action == "BatchReject"))
        };
    }

    public async Task<IReadOnlyList<TodoTaskDto>> ScanTimeoutsAsync()
    {
        var now = DateTime.UtcNow;
        var timeoutTasks = await _dbContext.TodoTasks
            .Where(t => t.Status == "Pending" && t.DueTime != null && t.DueTime < now)
            .ToListAsync();

        foreach (var task in timeoutTasks)
        {
            if (!task.ExtData?.Contains("\"isTimeout\":true") ?? true)
            {
                task.ExtData = MergeExtData(task.ExtData, "{\"isTimeout\":true}");
                await AddTaskLogAsync(task.Id, "TimeoutScan", "System", "System", "TimeoutDetected", "Pending", "Pending", "系统检测到任务已超时");
            }
        }

        await _dbContext.SaveChangesAsync();
        return timeoutTasks.Select(MapTask).ToList();
    }

    private static bool CanOperate(TodoTaskEntity? task) => task is not null && (task.Status == "Pending" || task.Status == "InProgress") && task.TaskCategory != "Copy";
    private static bool CanClaim(TodoTaskEntity? task) => task is not null && (task.Status == "Pending" || task.Status == "InProgress") && task.TaskCategory != "Copy" && string.IsNullOrEmpty(task.AssigneeId);

    private async Task<TodoTaskEntity?> FindExistingTaskAsync(CreateTodoTaskRequest request, string assigneeId)
    {
        if (string.IsNullOrWhiteSpace(request.ProcessInstanceId) || string.IsNullOrWhiteSpace(request.ProcessNodeId)) return null;

        return await _dbContext.TodoTasks.FirstOrDefaultAsync(t =>
            t.ProcessInstanceId == request.ProcessInstanceId &&
            t.ProcessNodeId == request.ProcessNodeId &&
            t.AssigneeId == assigneeId &&
            t.Status == "Pending" &&
            t.TaskCategory != "Copy");
    }

    private async Task<(string AssigneeId, string AssigneeName, string? AgentNote)> ResolveAssigneeAsync(string assigneeId, string assigneeName, string taskTypeCode)
    {
        var now = DateTime.UtcNow;
        var setting = await _dbContext.AgentSettings
            .Where(s => s.PrincipalUserId == assigneeId && s.Status == "Active" && s.StartTime <= now && s.EndTime >= now)
            .OrderByDescending(s => s.Id)
            .FirstOrDefaultAsync(s => s.ScopeType == "All" || s.TaskTypeCode == taskTypeCode);

        if (setting is null) return (assigneeId, assigneeName, null);

        return (setting.AgentUserId, setting.AgentUserId, JsonSerializer.Serialize(new { originalAssigneeId = assigneeId, originalAssigneeName = assigneeName, delegatedByRuleId = setting.Id }));
    }

    private async Task AddTaskLogAsync(long taskId, string action, string? operatorId, string? operatorName, string? actionResult, string? beforeStatus, string? afterStatus, string? comment, TodoTaskEntity? task = null)
    {
        _dbContext.TodoTaskLogs.Add(new TodoTaskLogEntity
        {
            TaskId = task == null ? taskId : 0,
            Task = task,
            Action = action,
            OperatorId = operatorId,
            OperatorName = operatorName,
            ActionResult = actionResult,
            Comment = comment,
            BeforeStatus = beforeStatus,
            AfterStatus = afterStatus,
            CreatedTime = DateTime.UtcNow
        });
        await Task.CompletedTask; // Keep it async for future
    }

    private static string? MergeExtData(string? original, string? addition)
    {
        if (string.IsNullOrWhiteSpace(addition)) return original;
        if (string.IsNullOrWhiteSpace(original)) return addition;
        return JsonSerializer.Serialize(new { original, addition });
    }

    private static TodoTaskDto MapTask(TodoTaskEntity task) => new()
    {
        Id = task.Id,
        TaskNo = task.TaskNo,
        TaskTypeCode = task.TaskTypeCode,
        Title = task.Title,
        BusinessSystem = task.BusinessSystem,
        BusinessType = task.BusinessType,
        BusinessId = task.BusinessId,
        ProcessInstanceId = task.ProcessInstanceId,
        ProcessNodeId = task.ProcessNodeId,
        AssigneeId = task.AssigneeId,
        AssigneeName = task.AssigneeName,
        TaskCategory = task.TaskCategory,
        Status = task.Status,
        Result = task.Result,
        Priority = task.Priority,
        DueTime = task.DueTime,
        CompletedTime = task.CompletedTime,
        ReadTime = task.ReadTime,
        OriginalTaskId = task.OriginalTaskId,
        IsClaimable = CanClaim(task),
        SourcePayload = task.SourcePayload,
        SourcePayloadData = ParseJson(task.SourcePayload),
        ExtData = task.ExtData,
        ExtDataObject = ParseJson(task.ExtData),
        CreatedBy = task.CreatedBy,
        CreatedTime = task.CreatedTime,
        UpdatedTime = task.UpdatedTime
    };

    private static TodoTaskLogDto MapTaskLog(TodoTaskLogEntity log) => new()
    {
        Id = log.Id,
        Action = log.Action,
        OperatorId = log.OperatorId,
        OperatorName = log.OperatorName,
        ActionResult = log.ActionResult,
        Comment = log.Comment,
        BeforeStatus = log.BeforeStatus,
        AfterStatus = log.AfterStatus,
        ExtData = log.ExtData,
        ExtDataObject = ParseJson(log.ExtData),
        CreatedTime = log.CreatedTime
    };

    private static TodoTaskNotifyLogDto MapNotifyLog(TodoTaskNotifyLogEntity log) => new()
    {
        Id = log.Id,
        NotifyType = log.NotifyType,
        ReceiverId = log.ReceiverId,
        ReceiverName = log.ReceiverName,
        Channel = log.Channel,
        Status = log.Status,
        Content = log.Content,
        SentTime = log.SentTime,
        CreatedTime = log.CreatedTime
    };

    private static JsonElement? ParseJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try { using var document = JsonDocument.Parse(json); return document.RootElement.Clone(); }
        catch (JsonException) { return null; }
    }

    private static AgentSettingDto MapAgentSetting(AgentSettingEntity setting) => new()
    {
        Id = setting.Id,
        PrincipalUserId = setting.PrincipalUserId,
        AgentUserId = setting.AgentUserId,
        ScopeType = setting.ScopeType,
        TaskTypeCode = setting.TaskTypeCode,
        StartTime = setting.StartTime,
        EndTime = setting.EndTime,
        Status = setting.Status,
        CreatedTime = setting.CreatedTime,
        UpdatedTime = setting.UpdatedTime
    };
}

public interface ITodoNotificationService
{
    Task SendAsync(TodoTaskDto task, string notifyType, string content);
}

public class DefaultNotificationService : ITodoNotificationService
{
    private readonly ILogger<DefaultNotificationService> _logger;

    public DefaultNotificationService(ILogger<DefaultNotificationService> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(TodoTaskDto task, string notifyType, string content)
    {
        _logger.LogInformation("【待办通知】类型: {NotifyType}, 接收人: {AssigneeName}({AssigneeId}), 任务: {Title}, 内容: {Content}",
            notifyType, task.AssigneeName, task.AssigneeId, task.Title, content);
        
        return Task.CompletedTask;
    }
}

public sealed class BatchCompleteResult
{
    public int CompletedCount { get; set; }
    public IReadOnlyList<TodoTaskDto> Tasks { get; set; } = [];
}

public sealed class BatchRejectResult
{
    public int RejectedCount { get; set; }
    public IReadOnlyList<TodoTaskDto> Tasks { get; set; } = [];
}

public sealed class BatchTransferResult
{
    public int TransferCount { get; set; }
    public IReadOnlyList<TodoTaskDto> OriginalTasks { get; set; } = [];
    public IReadOnlyList<TodoTaskDto> NewTasks { get; set; } = [];
}

public sealed class BatchUrgeResult
{
    public int UrgeCount { get; set; }
    public IReadOnlyList<TodoTaskNotifyLogDto> NotifyLogs { get; set; } = [];
}

public sealed class CancelByProcessNodeResult
{
    public int AffectedCount { get; set; }
    public IReadOnlyList<TodoTaskDto> Tasks { get; set; } = [];
}

public partial class Program
{
}
