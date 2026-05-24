using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ProcessCenter.API.Contracts;
using ProcessCenter.API.Services;

namespace ProcessCenter.API.Tests;

public sealed class ProcessCenterApiFactory : WebApplicationFactory<Program>
{
    public FakeTodoCenterClient FakeTodoCenterClient { get; } = new();
    public FakeBusinessCallbackClient FakeBusinessCallbackClient { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(ITodoCenterClient));
            services.RemoveAll(typeof(IBusinessCallbackClient));
            services.AddSingleton<ITodoCenterClient>(FakeTodoCenterClient);
            services.AddSingleton<IBusinessCallbackClient>(FakeBusinessCallbackClient);
        });
    }
}

public sealed class FakeTodoCenterClient : ITodoCenterClient
{
    public List<ProcessTodoCommandDto> CreatedTasks { get; } = [];
    public List<TodoCancelRecord> CanceledTasks { get; } = [];

    public Task<TodoTaskDispatchResult> CreateTaskAsync(ProcessTodoCommandDto command, CancellationToken cancellationToken = default)
    {
        CreatedTasks.Add(new ProcessTodoCommandDto
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
        });

        return Task.FromResult(new TodoTaskDispatchResult
        {
            Id = CreatedTasks.Count,
            TaskNo = $"TD-FAKE-{CreatedTasks.Count:D4}",
            Status = "Pending",
            AssigneeId = command.AssigneeId,
            AssigneeName = command.AssigneeName
        });
    }

    public Task CancelByProcessNodeAsync(
        string processInstanceId,
        string processNodeId,
        string targetStatus,
        string? operatorId,
        string? operatorName,
        string? comment,
        CancellationToken cancellationToken = default)
    {
        CanceledTasks.Add(new TodoCancelRecord(
            processInstanceId,
            processNodeId,
            targetStatus,
            operatorId,
            operatorName,
            comment));
        return Task.CompletedTask;
    }
}

public sealed class FakeBusinessCallbackClient : IBusinessCallbackClient
{
    public List<BusinessCallbackRecord> Calls { get; } = [];

    public Task NotifyApprovedAsync(
        ProcessInstanceDto instance,
        string? operatorId,
        string? operatorName,
        string? comment,
        CancellationToken cancellationToken = default)
    {
        Calls.Add(new BusinessCallbackRecord("Approved", instance.Id, instance.BusinessSystem, instance.BusinessType, instance.BusinessId, operatorId, operatorName, comment));
        return Task.CompletedTask;
    }

    public Task NotifyRejectedAsync(
        ProcessInstanceDto instance,
        string? operatorId,
        string? operatorName,
        string? comment,
        CancellationToken cancellationToken = default)
    {
        Calls.Add(new BusinessCallbackRecord("Rejected", instance.Id, instance.BusinessSystem, instance.BusinessType, instance.BusinessId, operatorId, operatorName, comment));
        return Task.CompletedTask;
    }
}

public sealed record BusinessCallbackRecord(
    string Action,
    long ProcessInstanceId,
    string BusinessSystem,
    string BusinessType,
    string BusinessId,
    string? OperatorId,
    string? OperatorName,
    string? Comment);

public sealed record TodoCancelRecord(
    string ProcessInstanceId,
    string ProcessNodeId,
    string TargetStatus,
    string? OperatorId,
    string? OperatorName,
    string? Comment);
