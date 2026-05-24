using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TodoCenter.API.Services;

namespace TodoCenter.API.Tests;

public class ProcessCenterHttpClientTests
{
    [Fact]
    public async Task CompleteTaskAsync_UsesVersionedProcessCallbackPath()
    {
        HttpRequestMessage? capturedRequest = null;
        var handler = new StubHttpMessageHandler(request =>
        {
            capturedRequest = request;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"code":200,"message":"success","data":{}}""", Encoding.UTF8, "application/json")
            };
        });

        using var httpClient = new HttpClient(handler);
        var client = new ProcessCenterHttpClient(
            httpClient,
            Options.Create(new ProcessCenterOptions
            {
                BaseUrl = "http://127.0.0.1:5099"
            }),
            Options.Create(new InternalCallbackOptions
            {
                SharedSecret = "internal-callback-secret"
            }));

        await client.CompleteTaskAsync("1001", "u1001", "管理员", "审批通过");

        Assert.NotNull(capturedRequest);
        Assert.Equal("/api/v1/process/complete-task", capturedRequest!.RequestUri!.PathAndQuery);
        Assert.True(capturedRequest.Headers.Contains(CallbackSecurityHeaders.EventId));
        Assert.True(capturedRequest.Headers.Contains(CallbackSecurityHeaders.Timestamp));
        Assert.True(capturedRequest.Headers.Contains(CallbackSecurityHeaders.Signature));

        var body = await capturedRequest.Content!.ReadAsStringAsync();
        using var json = JsonDocument.Parse(body);
        Assert.Equal(1001, json.RootElement.GetProperty("processInstanceId").GetInt64());
        Assert.Equal("Complete", json.RootElement.GetProperty("action").GetString());
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(responseFactory(request));
    }
}
