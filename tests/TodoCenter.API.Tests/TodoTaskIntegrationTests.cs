using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace TodoCenter.API.Tests;

public class TodoTaskIntegrationTests
{
    [Fact]
    public async Task TodoEndpoints_CreateCompleteAndSummarize_Succeeds()
    {
        await using var factory = new TodoCenterApiFactory();
        using var client = factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/todo/tasks", new
        {
            taskTypeCode = "PayrollApprove",
            title = "审批 2026-05 薪资",
            businessSystem = "HRMS",
            businessType = "PayrollRun",
            businessId = "RUN-2026-05",
            processInstanceId = "1001",
            processNodeId = "finance-approve",
            assigneeId = "u1001",
            assigneeName = "财务经理",
            priority = 1,
            sourcePayload = "{\"scene\":\"PayrollApproval\",\"yearMonth\":\"2026-05\",\"totalNetSalary\":12345.67}"
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var createJson = await createResponse.ReadJsonAsync();
        var taskId = createJson["data"]?["id"]?.GetValue<long>() ?? 0;
        Assert.True(taskId > 0);
        Assert.Equal("Pending", createJson["data"]?["status"]?.GetValue<string>());

        var myTasksResponse = await client.GetAsync("/api/todo/tasks/my-tasks?assigneeId=u1001&status=Pending&taskTypeCode=PayrollApprove&businessId=RUN-2026-05&keyword=2026-05");
        Assert.Equal(HttpStatusCode.OK, myTasksResponse.StatusCode);
        var myTasksJson = await myTasksResponse.ReadJsonAsync();
        Assert.Single(myTasksJson["data"]?.AsArray() ?? []);
        Assert.Equal("PayrollApproval", myTasksJson["data"]?[0]?["sourcePayloadData"]?["scene"]?.GetValue<string>());

        var detailResponse = await client.GetAsync($"/api/todo/tasks/{taskId}/detail");
        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
        var detailJson = await detailResponse.ReadJsonAsync();
        Assert.Equal("PayrollApproval", detailJson["data"]?["task"]?["sourcePayloadData"]?["scene"]?.GetValue<string>());
        Assert.Equal("2026-05", detailJson["data"]?["task"]?["sourcePayloadData"]?["yearMonth"]?.GetValue<string>());
        Assert.Single(detailJson["data"]?["logs"]?.AsArray() ?? []);
        Assert.Equal("Create", detailJson["data"]?["logs"]?[0]?["action"]?.GetValue<string>());

        var completeResponse = await client.PutAsJsonAsync($"/api/todo/tasks/{taskId}/complete", new
        {
            operatorId = "u1001",
            operatorName = "财务经理",
            comment = "审批通过"
        });

        Assert.Equal(HttpStatusCode.OK, completeResponse.StatusCode);
        var completeJson = await completeResponse.ReadJsonAsync();
        Assert.Equal("Completed", completeJson["data"]?["status"]?.GetValue<string>());
        Assert.Single(factory.FakeProcessCenterClient.Calls);
        Assert.Equal("Complete", factory.FakeProcessCenterClient.Calls[0].Action);
        Assert.Equal("1001", factory.FakeProcessCenterClient.Calls[0].ProcessInstanceId);
        Assert.Equal("u1001", factory.FakeProcessCenterClient.Calls[0].OperatorId);

        var completedDetailResponse = await client.GetAsync($"/api/todo/tasks/{taskId}/detail");
        Assert.Equal(HttpStatusCode.OK, completedDetailResponse.StatusCode);
        var completedDetailJson = await completedDetailResponse.ReadJsonAsync();
        Assert.Equal(2, completedDetailJson["data"]?["logs"]?.AsArray().Count);
        Assert.Equal("Complete", completedDetailJson["data"]?["logs"]?[0]?["action"]?.GetValue<string>());

        var summaryResponse = await client.GetAsync("/api/todo/kpi/summary?assigneeId=u1001");
        Assert.Equal(HttpStatusCode.OK, summaryResponse.StatusCode);
        var summaryJson = await summaryResponse.ReadJsonAsync();
        Assert.Equal(1, summaryJson["data"]?["totalCount"]?.GetValue<int>());
        Assert.Equal(1, summaryJson["data"]?["completedCount"]?.GetValue<int>());
        Assert.Equal(0, summaryJson["data"]?["pendingCount"]?.GetValue<int>());
    }

    [Fact]
    public async Task TodoEndpoints_RejectTask_NotifiesProcessCenter()
    {
        await using var factory = new TodoCenterApiFactory();
        using var client = factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/todo/tasks", new
        {
            taskTypeCode = "PayrollApprove",
            title = "审批 2026-05 薪资",
            businessSystem = "HRMS",
            businessType = "PayrollRun",
            businessId = "RUN-2026-05",
            processInstanceId = "1002",
            processNodeId = "manager-approve",
            assigneeId = "u1002",
            assigneeName = "部门经理",
            priority = 1
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var createJson = await createResponse.ReadJsonAsync();
        var taskId = createJson["data"]?["id"]?.GetValue<long>() ?? 0;
        Assert.True(taskId > 0);

        var rejectResponse = await client.PutAsJsonAsync($"/api/todo/tasks/{taskId}/reject", new
        {
            operatorId = "u1002",
            operatorName = "部门经理",
            comment = "驳回重提"
        });

        Assert.Equal(HttpStatusCode.OK, rejectResponse.StatusCode);
        var rejectJson = await rejectResponse.ReadJsonAsync();
        Assert.Equal("Rejected", rejectJson["data"]?["status"]?.GetValue<string>());
        Assert.Single(factory.FakeProcessCenterClient.Calls);
        Assert.Equal("Reject", factory.FakeProcessCenterClient.Calls[0].Action);
        Assert.Equal("1002", factory.FakeProcessCenterClient.Calls[0].ProcessInstanceId);
        Assert.Equal("u1002", factory.FakeProcessCenterClient.Calls[0].OperatorId);
    }

    [Fact]
    public async Task TodoEndpoints_BatchReject_NotifiesProcessCenterForEachTask()
    {
        await using var factory = new TodoCenterApiFactory();
        using var client = factory.CreateClient();

        foreach (var processInstanceId in new[] { "2001", "2002" })
        {
            var createResponse = await client.PostAsJsonAsync("/api/todo/tasks", new
            {
                taskTypeCode = "PayrollApprove",
                title = $"审批 {processInstanceId}",
                businessSystem = "HRMS",
                businessType = "PayrollRun",
                businessId = $"RUN-{processInstanceId}",
                processInstanceId,
                processNodeId = "finance-approve",
                assigneeId = "u1003",
                assigneeName = "财务经理"
            });

            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        }

        var batchRejectResponse = await client.PostAsJsonAsync("/api/todo/tasks/batch/reject", new
        {
            taskIds = new[] { 1, 2, 2 },
            operatorId = "u1003",
            operatorName = "财务经理",
            comment = "批量驳回"
        });

        Assert.Equal(HttpStatusCode.OK, batchRejectResponse.StatusCode);
        var batchRejectJson = await batchRejectResponse.ReadJsonAsync();
        Assert.Equal(2, batchRejectJson["data"]?["rejectedCount"]?.GetValue<int>());
        Assert.All(batchRejectJson["data"]?["tasks"]?.AsArray() ?? [], item =>
        {
            Assert.Equal("Rejected", item?["status"]?.GetValue<string>());
        });

        Assert.Equal(2, factory.FakeProcessCenterClient.Calls.Count);
        Assert.All(factory.FakeProcessCenterClient.Calls, call => Assert.Equal("Reject", call.Action));
        Assert.Contains(factory.FakeProcessCenterClient.Calls, call => call.ProcessInstanceId == "2001");
        Assert.Contains(factory.FakeProcessCenterClient.Calls, call => call.ProcessInstanceId == "2002");
    }

    [Fact]
    public async Task TodoEndpoints_AgentAndTransfer_Workflow_Succeeds()
    {
        await using var factory = new TodoCenterApiFactory();
        using var client = factory.CreateClient();

        var agentResponse = await client.PostAsJsonAsync("/api/todo/agent/settings", new
        {
            principalUserId = "u2001",
            agentUserId = "u3001",
            scopeType = "All",
            startTime = DateTime.UtcNow.AddHours(-1),
            endTime = DateTime.UtcNow.AddHours(2)
        });

        Assert.Equal(HttpStatusCode.Created, agentResponse.StatusCode);

        var createResponse = await client.PostAsJsonAsync("/api/todo/tasks", new
        {
            taskTypeCode = "ExpenseApprove",
            title = "审批差旅报销",
            businessSystem = "HRMS",
            businessType = "Expense",
            businessId = "EXP-1",
            assigneeId = "u2001",
            assigneeName = "原审批人"
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var createJson = await createResponse.ReadJsonAsync();
        var taskId = createJson["data"]?["id"]?.GetValue<long>() ?? 0;
        Assert.Equal("u3001", createJson["data"]?["assigneeId"]?.GetValue<string>());

        var transferResponse = await client.PutAsJsonAsync($"/api/todo/tasks/{taskId}/transfer", new
        {
            operatorId = "u3001",
            operatorName = "代理人",
            targetAssigneeId = "u4001",
            targetAssigneeName = "新审批人",
            comment = "转交处理"
        });

        Assert.Equal(HttpStatusCode.OK, transferResponse.StatusCode);
        var transferJson = await transferResponse.ReadJsonAsync();
        Assert.Equal("Transferred", transferJson["data"]?["originalTask"]?["status"]?.GetValue<string>());
        Assert.Equal("u4001", transferJson["data"]?["newTask"]?["assigneeId"]?.GetValue<string>());
        Assert.Equal("Pending", transferJson["data"]?["newTask"]?["status"]?.GetValue<string>());

        var settingsResponse = await client.GetAsync("/api/todo/agent/settings?principalUserId=u2001");
        Assert.Equal(HttpStatusCode.OK, settingsResponse.StatusCode);
        var settingsJson = await settingsResponse.ReadJsonAsync();
        Assert.Single(settingsJson["data"]?.AsArray() ?? []);
        Assert.Equal("u3001", settingsJson["data"]?[0]?["agentUserId"]?.GetValue<string>());
    }

    [Fact]
    public async Task TodoEndpoints_TaskDetail_IncludesNotifyLogs()
    {
        await using var factory = new TodoCenterApiFactory();
        using var client = factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/todo/tasks", new
        {
            taskTypeCode = "PayrollApprove",
            title = "审批 2026-06 薪资",
            businessSystem = "HRMS",
            businessType = "PayrollRun",
            businessId = "RUN-2026-06",
            assigneeId = "u5001",
            assigneeName = "财务主管"
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var createJson = await createResponse.ReadJsonAsync();
        var taskId = createJson["data"]?["id"]?.GetValue<long>() ?? 0;
        Assert.True(taskId > 0);

        var urgeResponse = await client.PostAsJsonAsync($"/api/todo/tasks/{taskId}/urge", new
        {
            operatorId = "u9001",
            operatorName = "管理员",
            comment = "请优先处理"
        });

        Assert.Equal(HttpStatusCode.OK, urgeResponse.StatusCode);

        var detailResponse = await client.GetAsync($"/api/todo/tasks/{taskId}/detail");
        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
        var detailJson = await detailResponse.ReadJsonAsync();
        Assert.Single(detailJson["data"]?["notifyLogs"]?.AsArray() ?? []);
        Assert.Equal("Urge", detailJson["data"]?["notifyLogs"]?[0]?["notifyType"]?.GetValue<string>());
        Assert.Equal("财务主管", detailJson["data"]?["notifyLogs"]?[0]?["receiverName"]?.GetValue<string>());
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
