using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using ProcessCenter.API.Services;

namespace ProcessCenter.API.Tests;

public class ProcessIntegrationTests
{
    [Fact]
    public async Task ProcessEndpoints_DefinitionStartAndComplete_Workflow_Succeeds()
    {
        await using var factory = new ProcessCenterApiFactory();
        using var client = factory.CreateClient();

        var definitionId = await CreateAndPublishDefinitionAsync(client, "PAYROLL_APPROVAL");

        Assert.True(definitionId > 0);

        var startResponse = await client.PostAsJsonAsync("/api/process/start", new
        {
            processCode = "PAYROLL_APPROVAL",
            businessSystem = "HRMS",
            businessType = "PayrollRun",
            businessId = "RUN-2026-05",
            title = "2026-05 薪资审批",
            starterId = "u0001",
            starterName = "发起人",
            requestId = "REQ-001",
            extData = JsonSerializer.Serialize(new
            {
                scene = "PayrollApproval",
                runId = "RUN-2026-05",
                yearMonth = "2026-05"
            })
        });

        Assert.Equal(HttpStatusCode.Created, startResponse.StatusCode);
        var startJson = await startResponse.ReadJsonAsync();
        var instanceId = startJson["data"]?["id"]?.GetValue<long>() ?? 0;
        Assert.True(instanceId > 0);
        Assert.Equal("Running", startJson["data"]?["status"]?.GetValue<string>());
        Assert.Equal("manager-approve", startJson["data"]?["currentNodeId"]?.GetValue<string>());
        Assert.Single(factory.FakeTodoCenterClient.CreatedTasks);
        Assert.Equal("manager-approve", factory.FakeTodoCenterClient.CreatedTasks[0].ProcessNodeId);
        Assert.Equal("u1001", factory.FakeTodoCenterClient.CreatedTasks[0].AssigneeId);
        Assert.Contains("\"scene\":\"PayrollApproval\"", factory.FakeTodoCenterClient.CreatedTasks[0].SourcePayload);
        Assert.Contains("\"yearMonth\":\"2026-05\"", factory.FakeTodoCenterClient.CreatedTasks[0].SourcePayload);

        var firstCompleteResponse = await PostSignedJsonAsync(
            client,
            "/api/process/complete-task",
            new
            {
                processInstanceId = instanceId,
                action = "Complete",
                operatorId = "u1001",
                operatorName = "部门经理",
                comment = "同意"
            });

        Assert.Equal(HttpStatusCode.OK, firstCompleteResponse.StatusCode);
        var firstCompleteJson = await firstCompleteResponse.ReadJsonAsync();
        Assert.Equal("finance-approve", firstCompleteJson["data"]?["instance"]?["currentNodeId"]?.GetValue<string>());
        Assert.Equal("u1002", firstCompleteJson["data"]?["nextTodo"]?["assigneeId"]?.GetValue<string>());
        Assert.Equal(2, factory.FakeTodoCenterClient.CreatedTasks.Count);
        Assert.Empty(factory.FakeBusinessCallbackClient.Calls);
        Assert.Equal("finance-approve", factory.FakeTodoCenterClient.CreatedTasks[1].ProcessNodeId);
        Assert.Equal("u1002", factory.FakeTodoCenterClient.CreatedTasks[1].AssigneeId);
        Assert.Contains("\"runId\":\"RUN-2026-05\"", factory.FakeTodoCenterClient.CreatedTasks[1].SourcePayload);

        var secondCompleteResponse = await PostSignedJsonAsync(
            client,
            "/api/process/complete-task",
            new
            {
                processInstanceId = instanceId,
                action = "Complete",
                operatorId = "u1002",
                operatorName = "财务经理",
                comment = "通过"
            });

        Assert.Equal(HttpStatusCode.OK, secondCompleteResponse.StatusCode);
        var secondCompleteJson = await secondCompleteResponse.ReadJsonAsync();
        Assert.Equal("Completed", secondCompleteJson["data"]?["instance"]?["status"]?.GetValue<string>());
        Assert.Single(factory.FakeBusinessCallbackClient.Calls);
        Assert.Equal("Approved", factory.FakeBusinessCallbackClient.Calls[0].Action);
        Assert.Equal(instanceId, factory.FakeBusinessCallbackClient.Calls[0].ProcessInstanceId);
        Assert.Equal("PayrollRun", factory.FakeBusinessCallbackClient.Calls[0].BusinessType);
        Assert.Equal("u1002", factory.FakeBusinessCallbackClient.Calls[0].OperatorId);

        var detailResponse = await client.GetAsync($"/api/process/instance/{instanceId}");
        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
        var detailJson = await detailResponse.ReadJsonAsync();
        Assert.Equal(2, detailJson["data"]?["history"]?.AsArray().Count);
        Assert.Null(detailJson["data"]?["currentTodo"]);
    }

    [Fact]
    public async Task ProcessEndpoints_RejectTask_TriggersBusinessCallback()
    {
        await using var factory = new ProcessCenterApiFactory();
        using var client = factory.CreateClient();

        await CreateAndPublishDefinitionAsync(client, "PAYROLL_APPROVAL");

        var startResponse = await client.PostAsJsonAsync("/api/process/start", new
        {
            processCode = "PAYROLL_APPROVAL",
            businessSystem = "HRMS",
            businessType = "PayrollRun",
            businessId = Guid.NewGuid().ToString(),
            title = "2026-05 薪资审批",
            starterId = "u0001",
            starterName = "发起人",
            requestId = "REQ-REJECT-001"
        });

        Assert.Equal(HttpStatusCode.Created, startResponse.StatusCode);
        var startJson = await startResponse.ReadJsonAsync();
        var instanceId = startJson["data"]?["id"]?.GetValue<long>() ?? 0;
        Assert.True(instanceId > 0);

        var rejectResponse = await PostSignedJsonAsync(
            client,
            "/api/process/complete-task",
            new
            {
                processInstanceId = instanceId,
                action = "Reject",
                operatorId = "u1001",
                operatorName = "部门经理",
                comment = "驳回"
            });

        Assert.Equal(HttpStatusCode.OK, rejectResponse.StatusCode);
        var rejectJson = await rejectResponse.ReadJsonAsync();
        Assert.Equal("Rejected", rejectJson["data"]?["instance"]?["status"]?.GetValue<string>());
        Assert.Single(factory.FakeBusinessCallbackClient.Calls);
        Assert.Equal("Rejected", factory.FakeBusinessCallbackClient.Calls[0].Action);
        Assert.Equal(instanceId, factory.FakeBusinessCallbackClient.Calls[0].ProcessInstanceId);
        Assert.Equal("u1001", factory.FakeBusinessCallbackClient.Calls[0].OperatorId);
    }

    [Fact]
    public async Task ProcessEndpoints_StartRequest_IsIdempotent()
    {
        await using var factory = new ProcessCenterApiFactory();
        using var client = factory.CreateClient();

        await CreateAndPublishDefinitionAsync(client, "EXPENSE_APPROVAL");

        var payload = new
        {
            processCode = "EXPENSE_APPROVAL",
            businessSystem = "HRMS",
            businessType = "Expense",
            businessId = "EXP-202605",
            title = "差旅报销审批",
            starterId = "u0002",
            starterName = "申请人",
            requestId = "REQ-EXP-001"
        };

        var firstResponse = await client.PostAsJsonAsync("/api/process/start", payload);
        var secondResponse = await client.PostAsJsonAsync("/api/process/start", payload);

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);

        var firstJson = await firstResponse.ReadJsonAsync();
        var secondJson = await secondResponse.ReadJsonAsync();
        Assert.Equal(
            firstJson["data"]?["id"]?.GetValue<long>(),
            secondJson["data"]?["id"]?.GetValue<long>());
        Assert.Single(factory.FakeTodoCenterClient.CreatedTasks);
    }

    [Fact]
    public async Task ProcessEndpoints_QueryInstances_ReturnsFilteredInstances()
    {
        await using var factory = new ProcessCenterApiFactory();
        using var client = factory.CreateClient();

        await CreateAndPublishDefinitionAsync(client, "PAYROLL_APPROVAL");

        var startResponse = await client.PostAsJsonAsync("/api/process/start", new
        {
            processCode = "PAYROLL_APPROVAL",
            businessSystem = "HRMS",
            businessType = "PayrollRun",
            businessId = "RUN-2026-06",
            title = "2026-06 薪资审批",
            starterId = "u0003",
            starterName = "发起人甲",
            requestId = "REQ-LIST-001"
        });

        Assert.Equal(HttpStatusCode.Created, startResponse.StatusCode);

        var listResponse = await client.GetAsync("/api/process/instances?processCode=PAYROLL_APPROVAL&status=Running&businessType=PayrollRun&keyword=2026-06");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var listJson = await listResponse.ReadJsonAsync();
        Assert.Single(listJson["data"]?.AsArray() ?? []);
        Assert.Equal("RUN-2026-06", listJson["data"]?[0]?["businessId"]?.GetValue<string>());
        Assert.Equal("发起人甲", listJson["data"]?[0]?["starterName"]?.GetValue<string>());
        Assert.Equal("Running", listJson["data"]?[0]?["status"]?.GetValue<string>());
        Assert.NotNull(listJson["data"]?[0]?["currentTodo"]);
    }

    [Fact]
    public async Task ProcessEndpoints_RebuildCurrentTodo_ReDispatchesCurrentTask()
    {
        await using var factory = new ProcessCenterApiFactory();
        using var client = factory.CreateClient();

        await CreateAndPublishDefinitionAsync(client, "PAYROLL_APPROVAL");

        var startResponse = await client.PostAsJsonAsync("/api/process/start", new
        {
            processCode = "PAYROLL_APPROVAL",
            businessSystem = "HRMS",
            businessType = "PayrollRun",
            businessId = "RUN-2026-07",
            title = "2026-07 薪资审批",
            starterId = "u0005",
            starterName = "发起人乙",
            requestId = "REQ-REBUILD-001"
        });

        Assert.Equal(HttpStatusCode.Created, startResponse.StatusCode);
        var startJson = await startResponse.ReadJsonAsync();
        var instanceId = startJson["data"]?["id"]?.GetValue<long>() ?? 0;
        Assert.True(instanceId > 0);
        Assert.Single(factory.FakeTodoCenterClient.CreatedTasks);

        var rebuildResponse = await client.PostAsJsonAsync($"/api/process/instance/{instanceId}/rebuild-current-todo", new
        {
            operatorId = "u9001",
            operatorName = "运维管理员",
            comment = "重建待办"
        });

        Assert.Equal(HttpStatusCode.OK, rebuildResponse.StatusCode);
        var rebuildJson = await rebuildResponse.ReadJsonAsync();
        Assert.Equal("Running", rebuildJson["data"]?["instance"]?["status"]?.GetValue<string>());
        Assert.Equal("manager-approve", rebuildJson["data"]?["currentTodo"]?["processNodeId"]?.GetValue<string>());
        Assert.Equal("TD-FAKE-0002", rebuildJson["data"]?["task"]?["taskNo"]?.GetValue<string>());
        Assert.Equal(2, factory.FakeTodoCenterClient.CreatedTasks.Count);
        Assert.Equal("u9001", factory.FakeTodoCenterClient.CreatedTasks[1].CreatedBy);
    }

    private static async Task<long> CreateAndPublishDefinitionAsync(HttpClient client, string processCode)
    {
        var createResponse = await client.PostAsJsonAsync("/api/process/definitions", new
        {
            processCode,
            processName = $"{processCode}-流程",
            businessType = processCode.Contains("PAYROLL", StringComparison.OrdinalIgnoreCase) ? "PayrollRun" : "Expense",
            nodes = new[]
            {
                new
                {
                    nodeId = "manager-approve",
                    nodeName = "部门经理审批",
                    nodeType = "UserTask",
                    assigneeId = "u1001",
                    assigneeName = "部门经理"
                },
                new
                {
                    nodeId = "finance-approve",
                    nodeName = "财务审批",
                    nodeType = "UserTask",
                    assigneeId = "u1002",
                    assigneeName = "财务经理"
                }
            }
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var createJson = await createResponse.ReadJsonAsync();
        var definitionId = createJson["data"]?["id"]?.GetValue<long>() ?? 0;

        var publishResponse = await client.PutAsync($"/api/process/definitions/{definitionId}/publish", null);
        Assert.Equal(HttpStatusCode.OK, publishResponse.StatusCode);
        return definitionId;
    }

    private static Task<HttpResponseMessage> PostSignedJsonAsync(HttpClient client, string path, object payload)
    {
        var payloadJson = JsonSerializer.Serialize(payload);
        var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = System.Net.Http.Json.JsonContent.Create(payload)
        };

        var eventId = Guid.NewGuid().ToString("N");
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var signature = CallbackSecurityService.CreateSignature(
            "internal-callback-secret",
            eventId,
            timestamp,
            path,
            payloadJson);

        request.Headers.Add(CallbackSecurityHeaders.EventId, eventId);
        request.Headers.Add(CallbackSecurityHeaders.Timestamp, timestamp);
        request.Headers.Add(CallbackSecurityHeaders.Signature, signature);

        return client.SendAsync(request);
    }
}

internal static class HttpResponseMessageExtensions
{
    public static async Task<JsonNode> ReadJsonAsync(this HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonNode.Parse(content, new JsonNodeOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidOperationException("响应不是有效 JSON。");
    }
}
