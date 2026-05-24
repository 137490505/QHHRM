using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TodoCenter.API.Services;

namespace TodoCenter.API.Tests;

public sealed class TodoCenterApiFactory : WebApplicationFactory<Program>
{
    public FakeProcessCenterClient FakeProcessCenterClient { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(IProcessCenterClient));
            services.AddSingleton<IProcessCenterClient>(FakeProcessCenterClient);
        });
    }
}

public sealed class FakeProcessCenterClient : IProcessCenterClient
{
    public List<ProcessCenterTaskActionRecord> Calls { get; } = [];

    public Task CompleteTaskAsync(string processInstanceId, string? operatorId, string? operatorName, string? comment, CancellationToken cancellationToken = default)
    {
        Calls.Add(new ProcessCenterTaskActionRecord("Complete", processInstanceId, operatorId, operatorName, comment));
        return Task.CompletedTask;
    }

    public Task RejectTaskAsync(string processInstanceId, string? operatorId, string? operatorName, string? comment, CancellationToken cancellationToken = default)
    {
        Calls.Add(new ProcessCenterTaskActionRecord("Reject", processInstanceId, operatorId, operatorName, comment));
        return Task.CompletedTask;
    }
}

public sealed record ProcessCenterTaskActionRecord(
    string Action,
    string ProcessInstanceId,
    string? OperatorId,
    string? OperatorName,
    string? Comment);
