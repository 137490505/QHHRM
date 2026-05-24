using System.Net;
using System.Text;
using System.Text.Json;
using HRMS.API.Services;
using Microsoft.Extensions.Options;

namespace HRMS.API.Tests;

public class WorkflowCenterGatewayTests
{
    [Fact]
    public async Task TodoCenterGateway_DownstreamReturnsNullBody_ReturnsNullJsonElement()
    {
        using var httpClient = new HttpClient(new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("null", Encoding.UTF8, "application/json")
            }));

        var gateway = new TodoCenterGateway(
            httpClient,
            Options.Create(new TodoCenterOptions
            {
                BaseUrl = "http://127.0.0.1:5163"
            }));

        var result = await gateway.GetKpiSummaryAsync("u1001");

        Assert.Equal(JsonValueKind.Null, result.ValueKind);
    }

    [Fact]
    public async Task TodoCenterGateway_GetKpiSummary_UsesVersionedTodoPath()
    {
        HttpRequestMessage? capturedRequest = null;

        using var httpClient = new HttpClient(new StubHttpMessageHandler(request =>
        {
            capturedRequest = request;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"code":200,"message":"success","data":{"totalCount":1}}""", Encoding.UTF8, "application/json")
            };
        }));

        var gateway = new TodoCenterGateway(
            httpClient,
            Options.Create(new TodoCenterOptions
            {
                BaseUrl = "http://127.0.0.1:5163"
            }));

        await gateway.GetKpiSummaryAsync("u1001");

        Assert.NotNull(capturedRequest);
        Assert.Equal("/api/v1/todo/kpi/summary?assigneeId=u1001", capturedRequest!.RequestUri!.PathAndQuery);
    }

    [Fact]
    public async Task ProcessCenterGateway_InstanceAndCallbackRoutes_UseSingularInstancePath()
    {
        var capturedPaths = new List<string>();

        using var httpClient = new HttpClient(new StubHttpMessageHandler(request =>
        {
            capturedPaths.Add(request.RequestUri!.PathAndQuery);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"code":200,"message":"success","data":{}}""", Encoding.UTF8, "application/json")
            };
        }));

        var gateway = new WorkflowProcessCenterGateway(
            httpClient,
            Options.Create(new ProcessCenterOptions
            {
                BaseUrl = "http://127.0.0.1:5099"
            }));

        await gateway.GetInstanceAsync(1001);
        await gateway.GetCallbackLogsAsync(1001);
        await gateway.RetryCallbackAsync(1001, 9001);
        await gateway.RejectInstanceAsync(1001, new ProcessInstanceActionDto());
        await gateway.TerminateInstanceAsync(1001, new ProcessInstanceActionDto());
        await gateway.RebuildCurrentTodoAsync(1001, new ProcessInstanceActionDto());
        await gateway.GetDefinitionAsync(11);
        await gateway.PublishDefinitionAsync(11);

        Assert.Contains("/api/v1/process/instance/1001", capturedPaths);
        Assert.Contains("/api/v1/process/instance/1001/callback-logs", capturedPaths);
        Assert.Contains("/api/v1/process/instance/1001/callback-logs/9001/retry", capturedPaths);
        Assert.Contains("/api/v1/process/instance/1001/reject", capturedPaths);
        Assert.Contains("/api/v1/process/instance/1001/terminate", capturedPaths);
        Assert.Contains("/api/v1/process/instance/1001/rebuild-current-todo", capturedPaths);
        Assert.Contains("/api/v1/process/definitions/11", capturedPaths);
        Assert.Contains("/api/v1/process/definitions/11/publish", capturedPaths);
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(responseFactory(request));
    }
}
