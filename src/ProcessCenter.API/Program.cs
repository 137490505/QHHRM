using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using ProcessCenter.API.Contracts;
using ProcessCenter.API.Data;
using ProcessCenter.API.Models;
using ProcessCenter.API.Services;

var builder = WebApplication.CreateBuilder(args);
var appStartedAt = DateTimeOffset.UtcNow;

if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_URLS")))
{
    builder.WebHost.UseUrls("http://localhost:5099");
}

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ProcessDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

builder.Services.AddScoped<ProcessService>();
builder.Services.AddScoped<ProcessedCallbackEventStore>();
builder.Services.AddScoped<CallbackSecurityService>();
builder.Services.Configure<TodoCenterOptions>(builder.Configuration.GetSection("TodoCenter"));
builder.Services.Configure<BusinessCallbackOptions>(builder.Configuration.GetSection("BusinessCallbacks"));
builder.Services.Configure<InternalCallbackOptions>(builder.Configuration.GetSection("InternalCallbacks"));
builder.Services.AddHttpClient<ITodoCenterClient, TodoCenterHttpClient>();
builder.Services.AddHttpClient<IBusinessCallbackClient, BusinessCallbackHttpClient>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/health", (IHostEnvironment environment) => Results.Ok(new
{
    status = "Healthy",
    service = "ProcessCenter.API",
    environment = environment.EnvironmentName,
    startedAt = appStartedAt,
    now = DateTimeOffset.UtcNow
}));

var processGroup = app.MapGroup("/api/v1/process");

processGroup.MapGet("/definitions", async (
    string? processCode,
    string? businessType,
    string? status,
    ProcessService service,
    HttpContext httpContext) =>
{
    return ApiResults.Ok(httpContext, await service.GetDefinitionsAsync(processCode, businessType, status));
});

processGroup.MapGet("/definitions/{id:long}", async (
    long id,
    ProcessService service,
    HttpContext httpContext) =>
{
    var definition = await service.GetDefinitionAsync(id);
    return definition is null
        ? ApiResults.NotFound(httpContext, "流程定义不存在")
        : ApiResults.Ok(httpContext, definition);
});

processGroup.MapPost("/definitions", async (
    CreateProcessDefinitionRequest request,
    ProcessService service,
    HttpContext httpContext) =>
{
    var validationResult = ValidationHelper.Validate(request);
    if (validationResult is not null)
    {
        return ApiResults.BadRequest(httpContext, "请求参数校验失败", validationResult);
    }

    var definition = await service.CreateDefinitionAsync(request);
    return definition is null
        ? ApiResults.BadRequest(httpContext, "流程编码已存在草稿版本")
        : ApiResults.Created(httpContext, definition, "流程定义创建成功");
});

processGroup.MapPut("/definitions/{id:long}", async (
    long id,
    CreateProcessDefinitionRequest request,
    ProcessService service,
    HttpContext httpContext) =>
{
    var validationResult = ValidationHelper.Validate(request);
    if (validationResult is not null)
    {
        return ApiResults.BadRequest(httpContext, "请求参数校验失败", validationResult);
    }

    var definition = await service.UpdateDefinitionAsync(id, request);
    return definition is null
        ? ApiResults.BadRequest(httpContext, "流程定义不存在、不是草稿，或目标流程编码已有其他草稿")
        : ApiResults.Ok(httpContext, definition, "流程定义更新成功");
});

processGroup.MapPut("/definitions/{id:long}/publish", async (
    long id,
    ProcessService service,
    HttpContext httpContext) =>
{
    var definition = await service.PublishDefinitionAsync(id);
    return definition is null
        ? ApiResults.NotFound(httpContext, "流程定义不存在")
        : ApiResults.Ok(httpContext, definition, "流程定义已发布");
});

processGroup.MapPost("/definitions/{id:long}/copy", async (
    long id,
    CopyProcessDefinitionRequest request,
    ProcessService service,
    HttpContext httpContext) =>
{
    var validationResult = ValidationHelper.Validate(request);
    if (validationResult is not null)
    {
        return ApiResults.BadRequest(httpContext, "请求参数校验失败", validationResult);
    }

    var definition = await service.CopyDefinitionAsync(id, request);
    return definition is null
        ? ApiResults.BadRequest(httpContext, "原流程定义不存在或目标流程编码已存在草稿")
        : ApiResults.Created(httpContext, definition, "流程定义复制成功");
});

processGroup.MapPut("/definitions/{id:long}/deactivate", async (
    long id,
    ProcessService service,
    HttpContext httpContext) =>
{
    var definition = await service.DeactivateDefinitionAsync(id);
    return definition is null
        ? ApiResults.NotFound(httpContext, "流程定义不存在")
        : ApiResults.Ok(httpContext, definition, "流程定义已停用");
});

processGroup.MapPost("/definitions/{id:long}/rollback", async (
    long id,
    RollbackProcessDefinitionRequest request,
    ProcessService service,
    HttpContext httpContext) =>
{
    var definition = await service.RollbackDefinitionAsync(id, request);
    return definition is null
        ? ApiResults.BadRequest(httpContext, "流程定义不存在或当前已有草稿版本")
        : ApiResults.Created(httpContext, definition, "流程定义已回滚为新草稿");
});

processGroup.MapPost("/start", async (
    StartProcessRequest request,
    ProcessService service,
    ITodoCenterClient todoCenterClient,
    HttpContext httpContext) =>
{
    var validationResult = ValidationHelper.Validate(request);
    if (validationResult is not null)
    {
        return ApiResults.BadRequest(httpContext, "请求参数校验失败", validationResult);
    }

    var result = await service.StartProcessAsync(request);
    if (result is not null && !result.IsDuplicate && result.Instance.CurrentTodo is not null)
    {
        try
        {
            await todoCenterClient.CreateTaskAsync(result.Instance.CurrentTodo, httpContext.RequestAborted);
        }
        catch (TodoCenterClientException exception)
        {
            return ApiResults.BadGateway(httpContext, "流程已启动，但创建待办失败", new
            {
                instance = result.Instance,
                error = exception.Message
            });
        }
    }

    return result switch
    {
        null => ApiResults.BadRequest(httpContext, "未找到已发布的流程定义"),
        { IsDuplicate: true } duplicate => ApiResults.Ok(httpContext, duplicate.Instance, "流程已按幂等键复用已有实例"),
        _ => ApiResults.Created(httpContext, result.Instance, "流程启动成功")
    };
});

processGroup.MapGet("/instances", async (
    string? processCode,
    string? status,
    string? businessType,
    string? businessId,
    string? keyword,
    ProcessService service,
    HttpContext httpContext) =>
{
    var instances = await service.QueryInstancesAsync(processCode, status, businessType, businessId, keyword);
    return ApiResults.Ok(httpContext, instances);
});

processGroup.MapGet("/instances/summary", async (
    string? processCode,
    string? businessType,
    ProcessService service,
    HttpContext httpContext) =>
{
    var summary = await service.GetInstanceSummaryAsync(processCode, businessType);
    return ApiResults.Ok(httpContext, summary);
});

processGroup.MapGet("/instance/{id:long}", async (
    long id,
    ProcessService service,
    HttpContext httpContext) =>
{
    var instance = await service.GetInstanceAsync(id);
    return instance is null
        ? ApiResults.NotFound(httpContext, "流程实例不存在")
        : ApiResults.Ok(httpContext, instance);
});

processGroup.MapGet("/instance/{id:long}/callback-logs", async (
    long id,
    ProcessService service,
    HttpContext httpContext) =>
{
    var logs = await service.GetCallbackLogsAsync(id);
    return ApiResults.Ok(httpContext, logs);
});

processGroup.MapPost("/instance/{id:long}/callback-logs/{logId:long}/retry", async (
    long id,
    long logId,
    ProcessService service,
    IBusinessCallbackClient businessCallbackClient,
    HttpContext httpContext) =>
{
    var log = await service.GetCallbackLogAsync(logId);
    if (log is null || log.ProcessInstanceId != id)
    {
        return ApiResults.NotFound(httpContext, "回调日志不存在");
    }

    var instance = await service.GetInstanceAsync(id);
    if (instance is null)
    {
        return ApiResults.NotFound(httpContext, "流程实例不存在");
    }

    // 这里需要一个能直接根据 URL 和 Payload 发送请求的方法
    // 或者重用 NotifyAsync 的逻辑
    // 为了简单，我们先标记日志并调用业务客户端
    
    var callbackResult = await businessCallbackClient.NotifyApprovedAsync(
        instance, 
        null, 
        "RetryJob", 
        "Manual Retry", 
        httpContext.RequestAborted);
    
    if (callbackResult != null)
    {
        await service.LogCallbackAsync(
            id,
            callbackResult.CallbackUrl,
            callbackResult.Payload,
            callbackResult.StatusCode,
            callbackResult.Response,
            callbackResult.IsSuccess ? "Success" : "Failed",
            callbackResult.ErrorMessage);
    }

    return callbackResult?.IsSuccess == true
        ? ApiResults.Ok(httpContext, callbackResult, "回调重试成功")
        : ApiResults.BadGateway(httpContext, "回调重试失败", callbackResult);
});

processGroup.MapPost("/complete-task", async (
    CompleteProcessTaskRequest request,
    ProcessService service,
    ITodoCenterClient todoCenterClient,
    IBusinessCallbackClient businessCallbackClient,
    CallbackSecurityService callbackSecurityService,
    HttpContext httpContext) =>
{
    var validationResult = ValidationHelper.Validate(request);
    if (validationResult is not null)
    {
        return ApiResults.BadRequest(httpContext, "请求参数校验失败", validationResult);
    }

    var callbackValidation = await callbackSecurityService.ValidateAsync(httpContext.Request, request);
    if (!callbackValidation.IsValid)
    {
        return Results.Json(
            ApiEnvelope.Failure(httpContext.TraceIdentifier, callbackValidation.StatusCode, callbackValidation.ErrorMessage ?? "回调校验失败"),
            statusCode: callbackValidation.StatusCode);
    }

    if (callbackValidation.IsDuplicate)
    {
        var duplicatedInstance = await service.GetInstanceAsync(request.ProcessInstanceId);
        return duplicatedInstance is null
            ? ApiResults.BadRequest(httpContext, "流程实例不存在或当前状态不可推进")
            : ApiResults.Ok(
                httpContext,
                new ProcessTaskTransitionResultDto
                {
                    Instance = duplicatedInstance,
                    NextTodo = null
                },
                "流程任务回调已幂等处理");
    }

    var result = await service.CompleteTaskAsync(request);
    if (result is null)
    {
        return ApiResults.BadRequest(httpContext, "流程实例不存在或当前状态不可推进");
    }

    BusinessCallbackResultDto? callbackResult = null;
    if (string.Equals(result.Instance.Status, "Completed", StringComparison.OrdinalIgnoreCase))
    {
        callbackResult = await businessCallbackClient.NotifyApprovedAsync(
            result.Instance,
            request.OperatorId,
            request.OperatorName,
            request.Comment,
            httpContext.RequestAborted);
    }
    else if (string.Equals(result.Instance.Status, "Rejected", StringComparison.OrdinalIgnoreCase))
    {
        callbackResult = await businessCallbackClient.NotifyRejectedAsync(
            result.Instance,
            request.OperatorId,
            request.OperatorName,
            request.Comment,
            httpContext.RequestAborted);
    }

    if (callbackResult != null)
    {
        await service.LogCallbackAsync(
            result.Instance.Id,
            callbackResult.CallbackUrl,
            callbackResult.Payload,
            callbackResult.StatusCode,
            callbackResult.Response,
            callbackResult.IsSuccess ? "Success" : "Failed",
            callbackResult.ErrorMessage);
    }

    if (callbackResult != null && !callbackResult.IsSuccess)
    {
        return ApiResults.BadGateway(httpContext, "流程状态已更新，但业务回调失败", new
        {
            instance = result.Instance,
            error = callbackResult.ErrorMessage
        });
    }

    if (result.NextTodo is not null)
    {
        try
        {
            await todoCenterClient.CreateTaskAsync(result.NextTodo, httpContext.RequestAborted);
        }
        catch (TodoCenterClientException exception)
        {
            return ApiResults.BadGateway(httpContext, "流程节点已推进，但创建下一待办失败", new
            {
                instance = result.Instance,
                nextTodo = result.NextTodo,
                error = exception.Message
            });
        }
    }

    if (callbackValidation.EventId is not null)
    {
        await callbackSecurityService.MarkProcessedAsync(callbackValidation.EventId);
    }

    return ApiResults.Ok(httpContext, result, "流程节点处理成功");
});

processGroup.MapPost("/instance/{id:long}/reject", async (
    long id,
    ProcessInstanceActionRequest request,
    ProcessService service,
    ITodoCenterClient todoCenterClient,
    IBusinessCallbackClient businessCallbackClient,
    HttpContext httpContext) =>
{
    var result = await service.RejectInstanceAsync(id, request);
    if (result is null)
    {
        return ApiResults.BadRequest(httpContext, "流程实例不存在或当前状态不可驳回");
    }

    if (!string.IsNullOrWhiteSpace(result.CurrentNodeId))
    {
        try
        {
            await todoCenterClient.CancelByProcessNodeAsync(
                result.Id.ToString(),
                result.CurrentNodeId,
                "Canceled",
                request.OperatorId,
                request.OperatorName,
                request.Comment,
                httpContext.RequestAborted);
        }
        catch (TodoCenterClientException exception)
        {
            return ApiResults.BadGateway(httpContext, "流程已驳回，但待办关闭失败", new
            {
                instance = result,
                error = exception.Message
            });
        }
    }

    var callbackResult = await businessCallbackClient.NotifyRejectedAsync(
        result,
        request.OperatorId,
        request.OperatorName,
        request.Comment,
        httpContext.RequestAborted);

    if (callbackResult != null)
    {
        await service.LogCallbackAsync(
            result.Id,
            callbackResult.CallbackUrl,
            callbackResult.Payload,
            callbackResult.StatusCode,
            callbackResult.Response,
            callbackResult.IsSuccess ? "Success" : "Failed",
            callbackResult.ErrorMessage);
    }

    if (callbackResult != null && !callbackResult.IsSuccess)
    {
        return ApiResults.BadGateway(httpContext, "流程已驳回，但业务回调失败", new
        {
            instance = result,
            error = callbackResult.ErrorMessage
        });
    }

    return ApiResults.Ok(httpContext, result, "流程已驳回");
});

processGroup.MapPost("/instance/{id:long}/terminate", async (
    long id,
    ProcessInstanceActionRequest request,
    ProcessService service,
    ITodoCenterClient todoCenterClient,
    HttpContext httpContext) =>
{
    var result = await service.TerminateInstanceAsync(id, request);
    if (result is null)
    {
        return ApiResults.BadRequest(httpContext, "流程实例不存在或当前状态不可终止");
    }

    if (!string.IsNullOrWhiteSpace(result.CurrentNodeId))
    {
        try
        {
            await todoCenterClient.CancelByProcessNodeAsync(
                result.Id.ToString(),
                result.CurrentNodeId,
                "Canceled",
                request.OperatorId,
                request.OperatorName,
                request.Comment,
                httpContext.RequestAborted);
        }
        catch (TodoCenterClientException exception)
        {
            return ApiResults.BadGateway(httpContext, "流程已终止，但待办关闭失败", new
            {
                instance = result,
                error = exception.Message
            });
        }
    }

    return ApiResults.Ok(httpContext, result, "流程已终止");
});

processGroup.MapPost("/instance/{id:long}/withdraw", async (
    long id,
    ProcessInstanceActionRequest request,
    ProcessService service,
    ITodoCenterClient todoCenterClient,
    HttpContext httpContext) =>
{
    var result = await service.WithdrawInstanceAsync(id, request);
    if (result is null)
    {
        return ApiResults.BadRequest(httpContext, "流程实例不存在或当前状态不可撤回");
    }

    if (!string.IsNullOrWhiteSpace(result.CurrentNodeId))
    {
        try
        {
            await todoCenterClient.CancelByProcessNodeAsync(
                result.Id.ToString(),
                result.CurrentNodeId,
                "Canceled",
                request.OperatorId,
                request.OperatorName,
                request.Comment,
                httpContext.RequestAborted);
        }
        catch (TodoCenterClientException exception)
        {
            return ApiResults.BadGateway(httpContext, "流程已撤回，但待办关闭失败", new
            {
                instance = result,
                error = exception.Message
            });
        }
    }

    return ApiResults.Ok(httpContext, result, "流程已撤回");
});

processGroup.MapPost("/instance/{id:long}/return", async (
    long id,
    ProcessReturnRequest request,
    ProcessService service,
    ITodoCenterClient todoCenterClient,
    HttpContext httpContext) =>
{
    var beforeInstance = await service.GetInstanceAsync(id);
    if (beforeInstance is null || string.IsNullOrWhiteSpace(beforeInstance.CurrentNodeId))
    {
        return ApiResults.BadRequest(httpContext, "流程实例不存在或当前状态不可退回");
    }

    var result = await service.ReturnInstanceAsync(id, request);
    if (result is null)
    {
        return ApiResults.BadRequest(httpContext, "流程实例不存在或当前状态不可退回");
    }

    try
    {
        await todoCenterClient.CancelByProcessNodeAsync(
            result.Instance.Id.ToString(),
            beforeInstance.CurrentNodeId,
            "Returned",
            request.OperatorId,
            request.OperatorName,
            request.Comment,
            httpContext.RequestAborted);
    }
    catch (TodoCenterClientException exception)
    {
        return ApiResults.BadGateway(httpContext, "流程已退回，但原待办关闭失败", new
        {
            instance = result.Instance,
            error = exception.Message
        });
    }

    if (result.CurrentTodo is not null)
    {
        try
        {
            await todoCenterClient.CreateTaskAsync(result.CurrentTodo, httpContext.RequestAborted);
        }
        catch (TodoCenterClientException exception)
        {
            return ApiResults.BadGateway(httpContext, "流程已退回，但重建待办失败", new
            {
                instance = result.Instance,
                currentTodo = result.CurrentTodo,
                error = exception.Message
            });
        }
    }

    return ApiResults.Ok(httpContext, result, "流程已退回");
});

processGroup.MapPost("/instance/{id:long}/restore", async (
    long id,
    ProcessInstanceActionRequest request,
    ProcessService service,
    ITodoCenterClient todoCenterClient,
    HttpContext httpContext) =>
{
    var result = await service.RestoreInstanceAsync(id, request);
    if (result is null)
    {
        return ApiResults.BadRequest(httpContext, "流程实例不存在或当前状态不可恢复");
    }

    if (result.CurrentTodo is not null)
    {
        try
        {
            await todoCenterClient.CreateTaskAsync(result.CurrentTodo, httpContext.RequestAborted);
        }
        catch (TodoCenterClientException exception)
        {
            return ApiResults.BadGateway(httpContext, "流程已恢复，但重建待办失败", new
            {
                instance = result.Instance,
                currentTodo = result.CurrentTodo,
                error = exception.Message
            });
        }
    }

    return ApiResults.Ok(httpContext, result, "流程已恢复运行");
});

processGroup.MapPost("/instance/{id:long}/transfer-current", async (
    long id,
    ProcessTransferCurrentRequest request,
    ProcessService service,
    ITodoCenterClient todoCenterClient,
    HttpContext httpContext) =>
{
    var validationResult = ValidationHelper.Validate(request);
    if (validationResult is not null)
    {
        return ApiResults.BadRequest(httpContext, "请求参数校验失败", validationResult);
    }

    var beforeInstance = await service.GetInstanceAsync(id);
    if (beforeInstance is null || string.IsNullOrWhiteSpace(beforeInstance.CurrentNodeId))
    {
        return ApiResults.BadRequest(httpContext, "流程实例不存在或当前节点不可改派");
    }

    var result = await service.TransferCurrentAssigneeAsync(id, request);
    if (result is null)
    {
        return ApiResults.BadRequest(httpContext, "流程实例不存在或当前节点不可改派");
    }

    try
    {
        await todoCenterClient.CancelByProcessNodeAsync(
            result.Instance.Id.ToString(),
            beforeInstance.CurrentNodeId,
            "Transferred",
            request.OperatorId,
            request.OperatorName,
            request.Comment,
            httpContext.RequestAborted);
    }
    catch (TodoCenterClientException exception)
    {
        return ApiResults.BadGateway(httpContext, "当前节点已改派，但原待办关闭失败", new
        {
            instance = result.Instance,
            error = exception.Message
        });
    }

    if (result.CurrentTodo is not null)
    {
        try
        {
            await todoCenterClient.CreateTaskAsync(result.CurrentTodo, httpContext.RequestAborted);
        }
        catch (TodoCenterClientException exception)
        {
            return ApiResults.BadGateway(httpContext, "当前节点已改派，但新待办创建失败", new
            {
                instance = result.Instance,
                currentTodo = result.CurrentTodo,
                error = exception.Message
            });
        }
    }

    return ApiResults.Ok(httpContext, result, "当前节点处理人已改派");
});

processGroup.MapPost("/instance/{id:long}/rebuild-current-todo", async (
    long id,
    ProcessInstanceActionRequest request,
    ProcessService service,
    ITodoCenterClient todoCenterClient,
    HttpContext httpContext) =>
{
    var instance = await service.GetInstanceAsync(id);
    if (instance is null)
    {
        return ApiResults.NotFound(httpContext, "流程实例不存在");
    }

    if (!string.Equals(instance.Status, "Running", StringComparison.OrdinalIgnoreCase) || instance.CurrentTodo is null)
    {
        return ApiResults.BadRequest(httpContext, "当前流程不处于运行中，无法重建待办");
    }

    var todoCommand = instance.CurrentTodo;
    if (!string.IsNullOrWhiteSpace(request.OperatorId))
    {
        todoCommand.CreatedBy = request.OperatorId;
    }

    try
    {
        var task = await todoCenterClient.CreateTaskAsync(todoCommand, httpContext.RequestAborted);
        return ApiResults.Ok(httpContext, new ProcessTodoRebuildResultDto
        {
            Instance = instance,
            CurrentTodo = todoCommand,
            Task = new ProcessTodoDispatchDto
            {
                Id = task.Id,
                TaskNo = task.TaskNo,
                Status = task.Status,
                AssigneeId = task.AssigneeId,
                AssigneeName = task.AssigneeName
            }
        }, "当前待办已重建");
    }
    catch (TodoCenterClientException exception)
    {
        return ApiResults.BadGateway(httpContext, "流程仍在运行，但重建待办失败", new
        {
            instance,
            error = exception.Message
        });
    }
});

// 初始化常用流程定义
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ProcessDbContext>();
    await InitializeProcessDefinitionsAsync(dbContext);
}

app.Run();

static async Task InitializeProcessDefinitionsAsync(ProcessDbContext dbContext)
{
    // 检查是否已存在流程定义
    if (await dbContext.ProcessDefinitions.AnyAsync())
    {
        return;
    }

    var now = DateTime.UtcNow;

    // 1. 请假流程
    var leaveProcess = new ProcessDefinitionEntity
    {
        ProcessCode = "Leave",
        ProcessName = "请假申请",
        BusinessType = "Leave",
        VersionNo = 1,
        Status = "Published",
        CreatedTime = now,
        UpdatedTime = now,
        Nodes =
        [
            new ProcessDefinitionNodeEntity
            {
                NodeId = "Start",
                NodeName = "发起人",
                NodeType = "StartEvent",
                AssigneeType = "Initiator",
                AssigneeId = "",
                AssigneeName = "发起人",
                NextNodeId = "Approval1",
                SortOrder = 0
            },
            new ProcessDefinitionNodeEntity
            {
                NodeId = "Approval1",
                NodeName = "部门主管审批",
                NodeType = "UserTask",
                AssigneeType = "Role",
                AssigneeId = "dept_leader",
                AssigneeName = "部门主管",
                NextNodeId = "End",
                SortOrder = 1
            },
            new ProcessDefinitionNodeEntity
            {
                NodeId = "End",
                NodeName = "结束",
                NodeType = "EndEvent",
                AssigneeType = "User",
                AssigneeId = "",
                AssigneeName = "",
                SortOrder = 2
            }
        ]
    };

    // 2. 报销流程
    var expenseProcess = new ProcessDefinitionEntity
    {
        ProcessCode = "Expense",
        ProcessName = "报销申请",
        BusinessType = "Expense",
        VersionNo = 1,
        Status = "Published",
        CreatedTime = now,
        UpdatedTime = now,
        Nodes =
        [
            new ProcessDefinitionNodeEntity
            {
                NodeId = "Start",
                NodeName = "发起人",
                NodeType = "StartEvent",
                AssigneeType = "Initiator",
                AssigneeId = "",
                AssigneeName = "发起人",
                NextNodeId = "Approval1",
                SortOrder = 0
            },
            new ProcessDefinitionNodeEntity
            {
                NodeId = "Approval1",
                NodeName = "部门主管审批",
                NodeType = "UserTask",
                AssigneeType = "Role",
                AssigneeId = "dept_leader",
                AssigneeName = "部门主管",
                NextNodeId = "Approval2",
                SortOrder = 1
            },
            new ProcessDefinitionNodeEntity
            {
                NodeId = "Approval2",
                NodeName = "财务审核",
                NodeType = "UserTask",
                AssigneeType = "Role",
                AssigneeId = "finance",
                AssigneeName = "财务",
                NextNodeId = "End",
                SortOrder = 2
            },
            new ProcessDefinitionNodeEntity
            {
                NodeId = "End",
                NodeName = "结束",
                NodeType = "EndEvent",
                AssigneeType = "User",
                AssigneeId = "",
                AssigneeName = "",
                SortOrder = 3
            }
        ]
    };

    // 3. 费用申请流程
    var costProcess = new ProcessDefinitionEntity
    {
        ProcessCode = "Cost",
        ProcessName = "费用申请",
        BusinessType = "Cost",
        VersionNo = 1,
        Status = "Published",
        CreatedTime = now,
        UpdatedTime = now,
        Nodes =
        [
            new ProcessDefinitionNodeEntity
            {
                NodeId = "Start",
                NodeName = "发起人",
                NodeType = "StartEvent",
                AssigneeType = "Initiator",
                AssigneeId = "",
                AssigneeName = "发起人",
                NextNodeId = "Approval1",
                SortOrder = 0
            },
            new ProcessDefinitionNodeEntity
            {
                NodeId = "Approval1",
                NodeName = "部门主管审批",
                NodeType = "UserTask",
                AssigneeType = "Role",
                AssigneeId = "dept_leader",
                AssigneeName = "部门主管",
                NextNodeId = "Approval2",
                SortOrder = 1,
                Conditions =
                [
                    new ProcessNodeConditionEntity
                    {
                        ConditionExpression = "Amount <= 1000",
                        TargetNodeId = "End"
                    }
                ]
            },
            new ProcessDefinitionNodeEntity
            {
                NodeId = "Approval2",
                NodeName = "分公司经理审批",
                NodeType = "UserTask",
                AssigneeType = "Role",
                AssigneeId = "branch_manager",
                AssigneeName = "分公司经理",
                NextNodeId = "Approval3",
                SortOrder = 2,
                Conditions =
                [
                    new ProcessNodeConditionEntity
                    {
                        ConditionExpression = "Amount <= 5000",
                        TargetNodeId = "End"
                    }
                ]
            },
            new ProcessDefinitionNodeEntity
            {
                NodeId = "Approval3",
                NodeName = "总公司总经理审批",
                NodeType = "UserTask",
                AssigneeType = "Role",
                AssigneeId = "ceo",
                AssigneeName = "总经理",
                NextNodeId = "End",
                SortOrder = 3
            },
            new ProcessDefinitionNodeEntity
            {
                NodeId = "End",
                NodeName = "结束",
                NodeType = "EndEvent",
                AssigneeType = "User",
                AssigneeId = "",
                AssigneeName = "",
                SortOrder = 4
            }
        ]
    };

    // 保存到数据库
    dbContext.ProcessDefinitions.AddRange(leaveProcess, expenseProcess, costProcess);
    await dbContext.SaveChangesAsync();
}

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

public class ProcessService
{
    private readonly ProcessDbContext _dbContext;

    public ProcessService(ProcessDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<ProcessDefinitionDto>> GetDefinitionsAsync(string? processCode = null, string? businessType = null, string? status = null)
    {
        var query = _dbContext.ProcessDefinitions.Include(p => p.Nodes).ThenInclude(n => n.Conditions).AsQueryable();

        if (!string.IsNullOrWhiteSpace(processCode)) query = query.Where(p => p.ProcessCode == processCode);
        if (!string.IsNullOrWhiteSpace(businessType)) query = query.Where(p => p.BusinessType == businessType);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(p => p.Status == status);

        var results = await query
            .OrderBy(p => p.ProcessCode)
            .ThenByDescending(p => p.VersionNo)
            .ToListAsync();

        return results.Select(MapDefinition).ToList();
    }

    public async Task<ProcessDefinitionDto?> GetDefinitionAsync(long id)
    {
        var definition = await _dbContext.ProcessDefinitions
            .Include(p => p.Nodes)
            .ThenInclude(n => n.Conditions)
            .FirstOrDefaultAsync(p => p.Id == id);

        return definition is null ? null : MapDefinition(definition);
    }

    public async Task<ProcessDefinitionDto?> CreateDefinitionAsync(CreateProcessDefinitionRequest request)
    {
        if (await _dbContext.ProcessDefinitions.AnyAsync(p => p.ProcessCode == request.ProcessCode && p.Status == "Draft"))
        {
            return null;
        }

        var now = DateTime.UtcNow;
        var definition = new ProcessDefinitionEntity
        {
            ProcessCode = request.ProcessCode,
            ProcessName = request.ProcessName,
            BusinessType = request.BusinessType,
            CallbackConfig = request.CallbackConfig,
            VersionNo = 0,
            Status = "Draft",
            CreatedTime = now,
            UpdatedTime = now,
            Nodes = request.Nodes
                .Select((node, index) => new ProcessDefinitionNodeEntity
                {
                    NodeId = node.NodeId,
                    NodeName = node.NodeName,
                    NodeType = node.NodeType,
                    AssigneeType = node.AssigneeType,
                    AssigneeId = node.AssigneeId,
                    AssigneeName = node.AssigneeName,
                    MultiPersonType = node.MultiPersonType,
                    NextNodeId = node.NextNodeId,
                    SortOrder = index + 1,
                    Conditions = node.Conditions?.Select(c => new ProcessNodeConditionEntity
                    {
                        ConditionExpression = c.ConditionExpression,
                        TargetNodeId = c.TargetNodeId
                    }).ToList() ?? []
                })
                .ToList()
        };

        _dbContext.ProcessDefinitions.Add(definition);
        await _dbContext.SaveChangesAsync();
        return MapDefinition(definition);
    }

    public async Task<ProcessDefinitionDto?> UpdateDefinitionAsync(long id, CreateProcessDefinitionRequest request)
    {
        var definition = await _dbContext.ProcessDefinitions
            .Include(p => p.Nodes)
            .ThenInclude(n => n.Conditions)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (definition is null || !string.Equals(definition.Status, "Draft", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var duplicatedDraftExists = await _dbContext.ProcessDefinitions
            .AnyAsync(p => p.Id != id && p.ProcessCode == request.ProcessCode && p.Status == "Draft");
        if (duplicatedDraftExists)
        {
            return null;
        }

        var existingConditions = definition.Nodes.SelectMany(node => node.Conditions).ToList();
        if (existingConditions.Count > 0)
        {
            _dbContext.ProcessNodeConditions.RemoveRange(existingConditions);
        }

        if (definition.Nodes.Count > 0)
        {
            _dbContext.ProcessDefinitionNodes.RemoveRange(definition.Nodes);
        }

        definition.ProcessCode = request.ProcessCode;
        definition.ProcessName = request.ProcessName;
        definition.BusinessType = request.BusinessType;
        definition.CallbackConfig = request.CallbackConfig;
        definition.UpdatedTime = DateTime.UtcNow;
        definition.Nodes = request.Nodes
            .Select((node, index) => new ProcessDefinitionNodeEntity
            {
                NodeId = node.NodeId,
                NodeName = node.NodeName,
                NodeType = node.NodeType,
                AssigneeType = node.AssigneeType,
                AssigneeId = node.AssigneeId,
                AssigneeName = node.AssigneeName,
                MultiPersonType = node.MultiPersonType,
                NextNodeId = node.NextNodeId,
                SortOrder = index + 1,
                Conditions = node.Conditions?.Select(c => new ProcessNodeConditionEntity
                {
                    ConditionExpression = c.ConditionExpression,
                    TargetNodeId = c.TargetNodeId
                }).ToList() ?? []
            })
            .ToList();

        await _dbContext.SaveChangesAsync();
        return MapDefinition(definition);
    }

    public async Task<ProcessDefinitionDto?> CopyDefinitionAsync(long id, CopyProcessDefinitionRequest request)
    {
        var source = await _dbContext.ProcessDefinitions.Include(p => p.Nodes).ThenInclude(n => n.Conditions).FirstOrDefaultAsync(p => p.Id == id);
        if (source is null) return null;

        if (await _dbContext.ProcessDefinitions.AnyAsync(p => p.ProcessCode == request.ProcessCode && p.Status == "Draft"))
        {
            return null;
        }

        var now = DateTime.UtcNow;
        var definition = new ProcessDefinitionEntity
        {
            ProcessCode = request.ProcessCode,
            ProcessName = request.ProcessName,
            BusinessType = source.BusinessType,
            VersionNo = 0,
            Status = "Draft",
            CreatedTime = now,
            UpdatedTime = now,
            Nodes = source.Nodes
                .OrderBy(n => n.SortOrder)
                .Select(node => new ProcessDefinitionNodeEntity
                {
                    NodeId = node.NodeId,
                    NodeName = node.NodeName,
                    NodeType = node.NodeType,
                    AssigneeType = node.AssigneeType,
                    AssigneeId = node.AssigneeId,
                    AssigneeName = node.AssigneeName,
                    MultiPersonType = node.MultiPersonType,
                    NextNodeId = node.NextNodeId,
                    SortOrder = node.SortOrder,
                    Conditions = node.Conditions.Select(c => new ProcessNodeConditionEntity
                    {
                        ConditionExpression = c.ConditionExpression,
                        TargetNodeId = c.TargetNodeId
                    }).ToList()
                })
                .ToList()
        };

        _dbContext.ProcessDefinitions.Add(definition);
        await _dbContext.SaveChangesAsync();
        return MapDefinition(definition);
    }

    public async Task<ProcessDefinitionDto?> PublishDefinitionAsync(long id)
    {
        var definition = await _dbContext.ProcessDefinitions.Include(p => p.Nodes).FirstOrDefaultAsync(p => p.Id == id);
        if (definition is null) return null;

        if (definition.Status != "Published")
        {
            var latestVersion = await _dbContext.ProcessDefinitions
                .Where(p => p.ProcessCode == definition.ProcessCode)
                .MaxAsync(p => (int?)p.VersionNo) ?? 0;

            definition.VersionNo = latestVersion + 1;
            definition.Status = "Published";
            definition.UpdatedTime = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
        }

        return MapDefinition(definition);
    }

    public async Task<ProcessDefinitionDto?> DeactivateDefinitionAsync(long id)
    {
        var definition = await _dbContext.ProcessDefinitions.FindAsync(id);
        if (definition is null) return null;

        definition.Status = "Inactive";
        definition.UpdatedTime = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();
        return MapDefinition(definition);
    }

    public async Task<ProcessDefinitionDto?> RollbackDefinitionAsync(long id, RollbackProcessDefinitionRequest request)
    {
        var source = await _dbContext.ProcessDefinitions.Include(p => p.Nodes).ThenInclude(n => n.Conditions).FirstOrDefaultAsync(p => p.Id == id);
        if (source is null) return null;

        if (await _dbContext.ProcessDefinitions.AnyAsync(p => p.ProcessCode == source.ProcessCode && p.Status == "Draft"))
        {
            return null;
        }

        var now = DateTime.UtcNow;
        var definition = new ProcessDefinitionEntity
        {
            ProcessCode = source.ProcessCode,
            ProcessName = string.IsNullOrWhiteSpace(request.ProcessName) ? source.ProcessName : request.ProcessName.Trim(),
            BusinessType = source.BusinessType,
            VersionNo = source.VersionNo,
            Status = "Draft",
            CreatedTime = now,
            UpdatedTime = now,
            Nodes = source.Nodes
                .OrderBy(n => n.SortOrder)
                .Select(node => new ProcessDefinitionNodeEntity
                {
                    NodeId = node.NodeId,
                    NodeName = node.NodeName,
                    NodeType = node.NodeType,
                    AssigneeType = node.AssigneeType,
                    AssigneeId = node.AssigneeId,
                    AssigneeName = node.AssigneeName,
                    MultiPersonType = node.MultiPersonType,
                    NextNodeId = node.NextNodeId,
                    SortOrder = node.SortOrder,
                    Conditions = node.Conditions.Select(c => new ProcessNodeConditionEntity
                    {
                        ConditionExpression = c.ConditionExpression,
                        TargetNodeId = c.TargetNodeId
                    }).ToList()
                })
                .ToList()
        };

        _dbContext.ProcessDefinitions.Add(definition);
        await _dbContext.SaveChangesAsync();
        return MapDefinition(definition);
    }

    public async Task<StartProcessResult?> StartProcessAsync(StartProcessRequest request)
    {
        var definition = await _dbContext.ProcessDefinitions.Include(p => p.Nodes).ThenInclude(n => n.Conditions)
            .Where(p => p.ProcessCode == request.ProcessCode && p.Status == "Published")
            .OrderByDescending(p => p.VersionNo)
            .FirstOrDefaultAsync();

        if (definition is null) return null;

        // 幂等校验
        if (!string.IsNullOrWhiteSpace(request.RequestId))
        {
            var existingInstance = await _dbContext.ProcessInstances.FirstOrDefaultAsync(i =>
                i.BusinessType == request.BusinessType && i.BusinessId == request.BusinessId && i.ProcessCode == request.ProcessCode);
            // 这里简单模拟幂等，实际应该用更严谨的 RequestId 索引
            if (existingInstance != null)
            {
                return new StartProcessResult(await GetInstanceAsync(existingInstance.Id) ?? throw new InvalidOperationException(), IsDuplicate: true);
            }
        }

        var firstNode = definition.Nodes.OrderBy(n => n.SortOrder).First();
        var now = DateTime.UtcNow;
        var instance = new ProcessInstanceEntity
        {
            InstanceNo = $"PI{DateTime.UtcNow:yyyyMMddHHmmss}{Guid.NewGuid().ToString("N")[..6].ToUpper()}",
            ProcessCode = definition.ProcessCode,
            VersionNo = definition.VersionNo,
            BusinessSystem = request.BusinessSystem,
            BusinessType = request.BusinessType,
            BusinessId = request.BusinessId,
            Title = request.Title,
            Status = "Running",
            CurrentNodeId = firstNode.NodeId,
            CurrentNodeName = firstNode.NodeName,
            CurrentAssigneeId = firstNode.AssigneeId,
            CurrentAssigneeName = firstNode.AssigneeName,
            CurrentNodeIndex = 0,
            StarterId = request.StarterId,
            StarterName = request.StarterName,
            StartedTime = now,
            ExtData = request.ExtData
        };

        _dbContext.ProcessInstances.Add(instance);
        await AddHistoryAsync(instance, firstNode, "Arrive", "Pending", null, null, null, now, null);
        await _dbContext.SaveChangesAsync();

        return new StartProcessResult(await GetInstanceAsync(instance.Id) ?? throw new InvalidOperationException(), IsDuplicate: false);
    }

    public async Task<ProcessInstanceDto?> GetInstanceAsync(long id)
    {
        var instance = await _dbContext.ProcessInstances.FindAsync(id);
        if (instance is null) return null;

        var definition = await GetDefinitionInternalAsync(instance.ProcessCode, instance.VersionNo);
        var histories = await _dbContext.ProcessNodeHistories.Where(h => h.ProcessInstanceId == id).OrderBy(h => h.Id).ToListAsync();
        
        var currentNode = definition?.Nodes
            .OrderBy(n => n.SortOrder)
            .FirstOrDefault(n => n.NodeId == instance.CurrentNodeId);

        return new ProcessInstanceDto
        {
            Id = instance.Id,
            InstanceNo = instance.InstanceNo,
            ProcessCode = instance.ProcessCode,
            VersionNo = instance.VersionNo,
            BusinessSystem = instance.BusinessSystem,
            BusinessType = instance.BusinessType,
            BusinessId = instance.BusinessId,
            Title = instance.Title,
            Status = instance.Status,
            CallbackConfig = definition?.CallbackConfig,
            CurrentNodeId = instance.CurrentNodeId,
            CurrentNodeName = instance.CurrentNodeName,
            CurrentAssigneeId = instance.CurrentAssigneeId,
            CurrentAssigneeName = instance.CurrentAssigneeName,
            StarterId = instance.StarterId,
            StarterName = instance.StarterName,
            StartedTime = instance.StartedTime,
            FinishedTime = instance.FinishedTime,
            ExtData = instance.ExtData,
            CurrentTodo = instance.Status == "Running" && currentNode != null ? BuildTodoCommand(instance, currentNode) : null,
            History = histories.Select(MapHistory).ToList()
        };
    }

    public async Task<IReadOnlyList<ProcessInstanceDto>> QueryInstancesAsync(string? processCode, string? status, string? businessType, string? businessId, string? keyword)
    {
        var query = _dbContext.ProcessInstances.AsQueryable();

        if (!string.IsNullOrWhiteSpace(processCode)) query = query.Where(i => i.ProcessCode == processCode);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(i => i.Status == status);
        if (!string.IsNullOrWhiteSpace(businessType)) query = query.Where(i => i.BusinessType == businessType);
        if (!string.IsNullOrWhiteSpace(businessId)) query = query.Where(i => i.BusinessId == businessId);
        if (!string.IsNullOrWhiteSpace(keyword)) query = query.Where(i => i.Title.Contains(keyword) || i.InstanceNo.Contains(keyword) || i.BusinessId.Contains(keyword) || i.StarterName.Contains(keyword));

        var instances = await query.OrderByDescending(i => i.StartedTime).ToListAsync();
        var results = new List<ProcessInstanceDto>();
        foreach (var i in instances)
        {
            results.Add(await GetInstanceAsync(i.Id) ?? throw new InvalidOperationException());
        }
        return results;
    }

    public async Task<IReadOnlyList<ProcessCallbackLogEntity>> GetCallbackLogsAsync(long instanceId)
    {
        return await _dbContext.ProcessCallbackLogs
            .Where(l => l.ProcessInstanceId == instanceId)
            .OrderByDescending(l => l.Id)
            .ToListAsync();
    }

    public async Task<ProcessCallbackLogEntity?> GetCallbackLogAsync(long id)
    {
        return await _dbContext.ProcessCallbackLogs.FindAsync(id);
    }

    public async Task<ProcessInstanceSummaryDto> GetInstanceSummaryAsync(string? processCode, string? businessType)
    {
        var query = _dbContext.ProcessInstances.AsQueryable();
        if (!string.IsNullOrWhiteSpace(processCode)) query = query.Where(i => i.ProcessCode == processCode);
        if (!string.IsNullOrWhiteSpace(businessType)) query = query.Where(i => i.BusinessType == businessType);

        return new ProcessInstanceSummaryDto
        {
            TotalCount = await query.CountAsync(),
            RunningCount = await query.CountAsync(i => i.Status == "Running"),
            CompletedCount = await query.CountAsync(i => i.Status == "Completed"),
            RejectedCount = await query.CountAsync(i => i.Status == "Rejected"),
            TerminatedCount = await query.CountAsync(i => i.Status == "Terminated"),
            WithdrawnCount = await query.CountAsync(i => i.Status == "Withdrawn")
        };
    }

    public async Task<ProcessTaskTransitionResultDto?> CompleteTaskAsync(CompleteProcessTaskRequest request)
    {
        var instance = await _dbContext.ProcessInstances.FindAsync(request.ProcessInstanceId);
        if (!CanAdvance(instance)) return null;

        var definition = await GetDefinitionInternalAsync(instance!.ProcessCode, instance.VersionNo);
        if (definition is null) return null;

        var currentNode = definition.Nodes.OrderBy(n => n.SortOrder).ElementAt(instance.CurrentNodeIndex);
        var now = DateTime.UtcNow;
        var history = await _dbContext.ProcessNodeHistories
            .Where(h => h.ProcessInstanceId == instance.Id && h.NodeId == currentNode.NodeId)
            .OrderByDescending(h => h.Id)
            .FirstAsync();

        history.HandlerId = request.OperatorId;
        history.HandlerName = request.OperatorName;
        history.Comment = request.Comment;
        history.HandledTime = now;
        history.DurationMinutes = Math.Round((decimal)(now - history.ArrivedTime).TotalMinutes, 2);

        if (request.Action == "Reject")
        {
            history.Action = "Reject";
            history.ActionResult = "Rejected";
            instance.Status = "Rejected";
            instance.FinishedTime = now;
            await _dbContext.SaveChangesAsync();
            return new ProcessTaskTransitionResultDto { Instance = await GetInstanceAsync(instance.Id) ?? throw new InvalidOperationException(), NextTodo = null };
        }

        history.Action = "Complete";
        history.ActionResult = "Completed";

        // 处理会签/或签
        if (!string.IsNullOrWhiteSpace(currentNode.MultiPersonType) && currentNode.MultiPersonType != "None")
        {
            var allAssignees = currentNode.AssigneeId.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var handledAssignees = await _dbContext.ProcessNodeHistories
                .Where(h => h.ProcessInstanceId == instance.Id && h.NodeId == currentNode.NodeId && h.ActionResult == "Completed")
                .Select(h => h.HandlerId)
                .Distinct()
                .ToListAsync();

            if (currentNode.MultiPersonType == "All" && handledAssignees.Count < allAssignees.Length)
            {
                await _dbContext.SaveChangesAsync();
                return new ProcessTaskTransitionResultDto { Instance = await GetInstanceAsync(instance.Id) ?? throw new InvalidOperationException(), NextTodo = null };
            }
        }

        var nextNode = ResolveNextNode(definition, instance, currentNode, request.NextNodeId);
        if (nextNode is null)
        {
            instance.Status = "Completed";
            instance.FinishedTime = now;
            instance.CurrentAssigneeId = null;
            instance.CurrentAssigneeName = null;
            await _dbContext.SaveChangesAsync();
            return new ProcessTaskTransitionResultDto { Instance = await GetInstanceAsync(instance.Id) ?? throw new InvalidOperationException(), NextTodo = null };
        }

        instance.CurrentNodeIndex = definition.Nodes.OrderBy(n => n.SortOrder).ToList().FindIndex(n => n.NodeId == nextNode.NodeId);
        instance.CurrentNodeId = nextNode.NodeId;
        instance.CurrentNodeName = nextNode.NodeName;
        instance.CurrentAssigneeId = nextNode.AssigneeId;
        instance.CurrentAssigneeName = nextNode.AssigneeName;

        await AddHistoryAsync(instance, nextNode, "Arrive", "Pending", null, null, null, now, null);
        await _dbContext.SaveChangesAsync();

        return new ProcessTaskTransitionResultDto { Instance = await GetInstanceAsync(instance.Id) ?? throw new InvalidOperationException(), NextTodo = BuildTodoCommand(instance, nextNode) };
    }

    public async Task<ProcessInstanceDto?> RejectInstanceAsync(long id, ProcessInstanceActionRequest request)
    {
        var instance = await _dbContext.ProcessInstances.FindAsync(id);
        if (!CanAdvance(instance)) return null;

        var definition = await GetDefinitionInternalAsync(instance!.ProcessCode, instance.VersionNo);
        if (definition is null) return null;

        var currentNode = definition.Nodes.OrderBy(n => n.SortOrder).ElementAt(instance.CurrentNodeIndex);
        var now = DateTime.UtcNow;
        await AddHistoryAsync(instance, currentNode, "Reject", "Rejected", request.OperatorId, request.OperatorName, request.Comment, now, now);
        instance.Status = "Rejected";
        instance.FinishedTime = now;
        instance.CurrentAssigneeId = null;
        instance.CurrentAssigneeName = null;
        await _dbContext.SaveChangesAsync();
        return await GetInstanceAsync(instance.Id);
    }

    public async Task<ProcessInstanceDto?> TerminateInstanceAsync(long id, ProcessInstanceActionRequest request)
    {
        var instance = await _dbContext.ProcessInstances.FindAsync(id);
        if (!CanAdvance(instance)) return null;

        var definition = await GetDefinitionInternalAsync(instance!.ProcessCode, instance.VersionNo);
        if (definition is null) return null;

        var currentNode = definition.Nodes.OrderBy(n => n.SortOrder).ElementAt(instance.CurrentNodeIndex);
        var now = DateTime.UtcNow;
        await AddHistoryAsync(instance, currentNode, "Terminate", "Terminated", request.OperatorId, request.OperatorName, request.Comment, now, now);
        instance.Status = "Terminated";
        instance.FinishedTime = now;
        instance.CurrentAssigneeId = null;
        instance.CurrentAssigneeName = null;
        await _dbContext.SaveChangesAsync();
        return await GetInstanceAsync(instance.Id);
    }

    public async Task<ProcessInstanceDto?> WithdrawInstanceAsync(long id, ProcessInstanceActionRequest request)
    {
        var instance = await _dbContext.ProcessInstances.FindAsync(id);
        if (!CanAdvance(instance)) return null;

        var definition = await GetDefinitionInternalAsync(instance!.ProcessCode, instance.VersionNo);
        if (definition is null) return null;

        var currentNode = definition.Nodes.OrderBy(n => n.SortOrder).ElementAt(instance.CurrentNodeIndex);
        var currentHistory = await _dbContext.ProcessNodeHistories
            .Where(h => h.ProcessInstanceId == instance.Id && h.NodeId == currentNode.NodeId && h.Action == "Arrive")
            .OrderByDescending(h => h.Id)
            .FirstOrDefaultAsync();

        if (currentHistory?.HandlerId != null || currentHistory?.HandledTime != null) return null;

        var now = DateTime.UtcNow;
        await AddHistoryAsync(instance, currentNode, "Withdraw", "Withdrawn", request.OperatorId, request.OperatorName, request.Comment, now, now);
        instance.Status = "Withdrawn";
        instance.FinishedTime = now;
        instance.CurrentAssigneeId = null;
        instance.CurrentAssigneeName = null;
        await _dbContext.SaveChangesAsync();
        return await GetInstanceAsync(instance.Id);
    }

    public async Task<ProcessInstanceOperateResultDto?> ReturnInstanceAsync(long id, ProcessReturnRequest request)
    {
        var instance = await _dbContext.ProcessInstances.FindAsync(id);
        if (!CanAdvance(instance)) return null;

        var definition = await GetDefinitionInternalAsync(instance!.ProcessCode, instance.VersionNo);
        if (definition is null) return null;

        var orderedNodes = definition.Nodes.OrderBy(n => n.SortOrder).ToList();
        var targetIndex = ResolveReturnTargetIndex(orderedNodes, instance.CurrentNodeIndex, request.TargetNodeId);
        if (targetIndex is null) return null;

        var currentNode = orderedNodes[instance.CurrentNodeIndex];
        var targetNode = orderedNodes[targetIndex.Value];
        var now = DateTime.UtcNow;
        await AddHistoryAsync(instance, currentNode, "Return", "Returned", request.OperatorId, request.OperatorName, request.Comment, now, now);

        instance.CurrentNodeIndex = targetIndex.Value;
        instance.CurrentNodeId = targetNode.NodeId;
        instance.CurrentNodeName = targetNode.NodeName;
        instance.CurrentAssigneeId = targetNode.AssigneeId;
        instance.CurrentAssigneeName = targetNode.AssigneeName;
        instance.FinishedTime = null;

        await AddHistoryAsync(instance, targetNode, "Arrive", "Pending", null, null, request.Comment, now, null);
        await _dbContext.SaveChangesAsync();

        return new ProcessInstanceOperateResultDto { Instance = await GetInstanceAsync(instance.Id) ?? throw new InvalidOperationException(), CurrentTodo = BuildTodoCommand(instance, targetNode) };
    }

    public async Task<ProcessInstanceOperateResultDto?> RestoreInstanceAsync(long id, ProcessInstanceActionRequest request)
    {
        var instance = await _dbContext.ProcessInstances.FindAsync(id);
        if (instance is null || instance.Status == "Running") return null;

        var definition = await GetDefinitionInternalAsync(instance.ProcessCode, instance.VersionNo);
        if (definition is null) return null;

        var orderedNodes = definition.Nodes.OrderBy(n => n.SortOrder).ToList();
        if (instance.CurrentNodeIndex < 0 || instance.CurrentNodeIndex >= orderedNodes.Count)
        {
            instance.CurrentNodeIndex = Math.Max(0, orderedNodes.Count - 1);
        }

        var currentNode = orderedNodes[instance.CurrentNodeIndex];
        var now = DateTime.UtcNow;
        instance.Status = "Running";
        instance.FinishedTime = null;
        instance.CurrentNodeId = currentNode.NodeId;
        instance.CurrentNodeName = currentNode.NodeName;
        instance.CurrentAssigneeId = currentNode.AssigneeId;
        instance.CurrentAssigneeName = currentNode.AssigneeName;
        await AddHistoryAsync(instance, currentNode, "Restore", "Pending", request.OperatorId, request.OperatorName, request.Comment, now, null);
        await _dbContext.SaveChangesAsync();

        return new ProcessInstanceOperateResultDto { Instance = await GetInstanceAsync(instance.Id) ?? throw new InvalidOperationException(), CurrentTodo = BuildTodoCommand(instance, currentNode) };
    }

    public async Task<ProcessInstanceOperateResultDto?> TransferCurrentAssigneeAsync(long id, ProcessTransferCurrentRequest request)
    {
        var instance = await _dbContext.ProcessInstances.FindAsync(id);
        if (!CanAdvance(instance)) return null;

        var definition = await GetDefinitionInternalAsync(instance!.ProcessCode, instance.VersionNo);
        if (definition is null) return null;

        var orderedNodes = definition.Nodes.OrderBy(n => n.SortOrder).ToList();
        var currentNode = orderedNodes[instance.CurrentNodeIndex];
        instance.CurrentAssigneeId = request.TargetAssigneeId;
        instance.CurrentAssigneeName = request.TargetAssigneeName;
        await AddHistoryAsync(instance, currentNode, "TransferCurrent", "Transferred", request.OperatorId, request.OperatorName, request.Comment, DateTime.UtcNow, DateTime.UtcNow);
        await _dbContext.SaveChangesAsync();

        return new ProcessInstanceOperateResultDto { Instance = await GetInstanceAsync(instance.Id) ?? throw new InvalidOperationException(), CurrentTodo = BuildTodoCommand(instance, currentNode) };
    }

    public async Task LogCallbackAsync(long instanceId, string url, string payload, int statusCode, string? response, string status, string? errorMessage)
    {
        var log = new ProcessCallbackLogEntity
        {
            ProcessInstanceId = instanceId,
            CallbackUrl = url,
            Payload = payload,
            StatusCode = statusCode,
            Response = response,
            Status = status,
            ErrorMessage = errorMessage,
            CreatedTime = DateTime.UtcNow
        };
        _dbContext.ProcessCallbackLogs.Add(log);
        await _dbContext.SaveChangesAsync();
    }

    private static bool CanAdvance(ProcessInstanceEntity? instance) => instance is not null && instance.Status == "Running";

    private static int? ResolveReturnTargetIndex(IReadOnlyList<ProcessDefinitionNodeEntity> orderedNodes, int currentNodeIndex, string? targetNodeId)
    {
        if (!string.IsNullOrWhiteSpace(targetNodeId))
        {
            var targetIndex = orderedNodes.Select((n, i) => new { n.NodeId, Index = i }).FirstOrDefault(x => x.NodeId == targetNodeId)?.Index;
            return (targetIndex == null || targetIndex >= currentNodeIndex) ? null : targetIndex;
        }
        return currentNodeIndex > 0 ? currentNodeIndex - 1 : null;
    }

    private static ProcessDefinitionNodeEntity? ResolveNextNode(ProcessDefinitionEntity definition, ProcessInstanceEntity instance, ProcessDefinitionNodeEntity currentNode, string? requestedNextNodeId)
    {
        var orderedNodes = definition.Nodes.OrderBy(n => n.SortOrder).ToList();
        if (!string.IsNullOrWhiteSpace(requestedNextNodeId)) return orderedNodes.FirstOrDefault(n => n.NodeId == requestedNextNodeId);

        if (currentNode.Conditions.Count > 0)
        {
            foreach (var condition in currentNode.Conditions)
            {
                if (EvaluateCondition(condition.ConditionExpression, instance.ExtData)) return orderedNodes.FirstOrDefault(n => n.NodeId == condition.TargetNodeId);
            }
        }

        if (!string.IsNullOrWhiteSpace(currentNode.NextNodeId)) return orderedNodes.FirstOrDefault(n => n.NodeId == currentNode.NextNodeId);

        return instance.CurrentNodeIndex >= orderedNodes.Count - 1 ? null : orderedNodes[instance.CurrentNodeIndex + 1];
    }

    private static bool EvaluateCondition(string? expression, string? payload)
    {
        if (string.IsNullOrWhiteSpace(expression)) return true;
        if (string.IsNullOrWhiteSpace(payload)) return false;
        return payload.Contains(expression, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<ProcessDefinitionEntity?> GetDefinitionInternalAsync(string processCode, int versionNo)
    {
        return await _dbContext.ProcessDefinitions.Include(p => p.Nodes).ThenInclude(n => n.Conditions)
            .FirstOrDefaultAsync(p => p.ProcessCode == processCode && p.VersionNo == versionNo);
    }

    private async Task AddHistoryAsync(ProcessInstanceEntity instance, ProcessDefinitionNodeEntity node, string action, string actionResult, string? handlerId, string? handlerName, string? comment, DateTime arrivedTime, DateTime? handledTime)
    {
        var history = new ProcessNodeHistoryEntity
        {
            ProcessInstance = instance,
            NodeId = node.NodeId,
            NodeName = node.NodeName,
            NodeType = node.NodeType,
            HandlerId = handlerId,
            HandlerName = handlerName,
            Action = action,
            ActionResult = actionResult,
            Comment = comment,
            ArrivedTime = arrivedTime,
            HandledTime = handledTime,
            DurationMinutes = handledTime == null ? null : Math.Round((decimal)(handledTime.Value - arrivedTime).TotalMinutes, 2)
        };
        _dbContext.ProcessNodeHistories.Add(history);
        await Task.CompletedTask;
    }

    private static ProcessTodoCommandDto BuildTodoCommand(ProcessInstanceEntity instance, ProcessDefinitionNodeEntity node) => new()
    {
        TaskTypeCode = $"{instance.ProcessCode}.Approve",
        Title = $"{instance.Title} - {node.NodeName}",
        BusinessSystem = instance.BusinessSystem,
        BusinessType = instance.BusinessType,
        BusinessId = instance.BusinessId,
        ProcessInstanceId = instance.Id.ToString(),
        ProcessNodeId = node.NodeId,
        AssigneeId = node.AssigneeType == "Initiator" ? instance.StarterId : (instance.CurrentAssigneeId ?? node.AssigneeId),
        AssigneeName = node.AssigneeType == "Initiator" ? instance.StarterName : (instance.CurrentAssigneeName ?? node.AssigneeName),
        Priority = 2,
        SourcePayload = instance.ExtData,
        CreatedBy = instance.StarterId
    };

    private static ProcessDefinitionDto MapDefinition(ProcessDefinitionEntity definition) => new()
    {
        Id = definition.Id,
        ProcessCode = definition.ProcessCode,
        ProcessName = definition.ProcessName,
        BusinessType = definition.BusinessType,
        VersionNo = definition.VersionNo,
        Status = definition.Status,
        CallbackConfig = definition.CallbackConfig,
        CreatedTime = definition.CreatedTime,
        UpdatedTime = definition.UpdatedTime,
        Nodes = definition.Nodes.OrderBy(n => n.SortOrder).Select(node => new ProcessDefinitionNodeDto
        {
            NodeId = node.NodeId,
            NodeName = node.NodeName,
            NodeType = node.NodeType,
            AssigneeType = node.AssigneeType,
            AssigneeId = node.AssigneeId,
            AssigneeName = node.AssigneeName,
            MultiPersonType = node.MultiPersonType,
            NextNodeId = node.NextNodeId,
            SortOrder = node.SortOrder,
            Conditions = node.Conditions.Select(c => new ProcessNodeConditionDto { ConditionExpression = c.ConditionExpression, TargetNodeId = c.TargetNodeId }).ToList()
        }).ToList()
    };

    private static ProcessNodeHistoryDto MapHistory(ProcessNodeHistoryEntity history) => new()
    {
        Id = history.Id,
        NodeId = history.NodeId,
        NodeName = history.NodeName,
        NodeType = history.NodeType,
        HandlerId = history.HandlerId,
        HandlerName = history.HandlerName,
        Action = history.Action,
        ActionResult = history.ActionResult,
        Comment = history.Comment,
        ArrivedTime = history.ArrivedTime,
        HandledTime = history.HandledTime,
        DurationMinutes = history.DurationMinutes
    };
}

public sealed record StartProcessResult(ProcessInstanceDto Instance, bool IsDuplicate);

public partial class Program
{
}
