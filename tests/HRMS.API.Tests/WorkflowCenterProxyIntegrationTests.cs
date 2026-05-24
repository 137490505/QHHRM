using System.Net;
using System.Net.Http.Json;

namespace HRMS.API.Tests;

public class WorkflowCenterProxyIntegrationTests
{
    [Fact]
    public async Task TodoProxyEndpoints_QueryAndAction_Succeed()
    {
        await using var factory = new HrmsApiFactory();
        using var client = await factory.CreateAuthenticatedClientAsync();

        var listResponse = await client.GetAsync($"/api/v1/todo/tasks/my-tasks?assigneeId={factory.SeedState.AdminUserId}&status=Pending&taskTypeCode=PAYROLL_APPROVAL.Approve&keyword=2026-05");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var listJson = await listResponse.ReadJsonAsync();
        Assert.Single(listJson["data"]?.AsArray() ?? []);
        Assert.Equal("Pending", listJson["data"]?[0]?["status"]?.GetValue<string>());

        var detailResponse = await client.GetAsync("/api/v1/todo/tasks/1/detail");
        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
        var detailJson = await detailResponse.ReadJsonAsync();
        Assert.Equal("PayrollApproval", detailJson["data"]?["task"]?["sourcePayloadData"]?["scene"]?.GetValue<string>());

        var completeResponse = await client.PutAsJsonAsync("/api/v1/todo/tasks/1/complete", new
        {
            operatorId = factory.SeedState.AdminUserId.ToString(),
            operatorName = "管理员",
            comment = "审批通过"
        });
        Assert.Equal(HttpStatusCode.OK, completeResponse.StatusCode);
        var completeJson = await completeResponse.ReadJsonAsync();
        Assert.Equal("Completed", completeJson["data"]?["status"]?.GetValue<string>());

        var kpiResponse = await client.GetAsync($"/api/v1/todo/kpi/summary?assigneeId={factory.SeedState.AdminUserId}");
        Assert.Equal(HttpStatusCode.OK, kpiResponse.StatusCode);
        var kpiJson = await kpiResponse.ReadJsonAsync();
        Assert.Equal(3, kpiJson["data"]?["totalCount"]?.GetValue<int>());

        Assert.Contains(factory.FakeTodoCenterGateway.Calls, call => call.StartsWith("list:", StringComparison.Ordinal));
        Assert.Contains(factory.FakeTodoCenterGateway.Calls, call => call == "detail:1");
        Assert.Contains(factory.FakeTodoCenterGateway.Calls, call => call.StartsWith("complete:1:", StringComparison.Ordinal));
        Assert.Contains(factory.FakeTodoCenterGateway.Calls, call => call.StartsWith("kpi:", StringComparison.Ordinal));
    }

    [Fact]
    public async Task TodoProxyEndpoints_TransferBatchAndAgent_Succeed()
    {
        await using var factory = new HrmsApiFactory();
        using var client = await factory.CreateAuthenticatedClientAsync();

        var transferResponse = await client.PutAsJsonAsync("/api/v1/todo/tasks/1/transfer", new
        {
            operatorId = factory.SeedState.AdminUserId.ToString(),
            operatorName = "管理员",
            targetAssigneeId = "u3001",
            targetAssigneeName = "代理审批人",
            comment = "转交处理"
        });
        Assert.Equal(HttpStatusCode.OK, transferResponse.StatusCode);
        var transferJson = await transferResponse.ReadJsonAsync();
        Assert.Equal("Transferred", transferJson["data"]?["originalTask"]?["status"]?.GetValue<string>());

        var batchResponse = await client.PostAsJsonAsync("/api/v1/todo/tasks/batch/complete", new
        {
            taskIds = new[] { 1, 2 },
            operatorId = factory.SeedState.AdminUserId.ToString(),
            operatorName = "管理员",
            comment = "批量审批"
        });
        Assert.Equal(HttpStatusCode.OK, batchResponse.StatusCode);
        var batchJson = await batchResponse.ReadJsonAsync();
        Assert.Equal(2, batchJson["data"]?["completedCount"]?.GetValue<int>());

        var batchRejectResponse = await client.PostAsJsonAsync("/api/v1/todo/tasks/batch/reject", new
        {
            taskIds = new[] { 1, 2 },
            operatorId = factory.SeedState.AdminUserId.ToString(),
            operatorName = "管理员",
            comment = "批量驳回"
        });
        Assert.Equal(HttpStatusCode.OK, batchRejectResponse.StatusCode);
        var batchRejectJson = await batchRejectResponse.ReadJsonAsync();
        Assert.Equal(2, batchRejectJson["data"]?["rejectedCount"]?.GetValue<int>());

        var createAgentResponse = await client.PostAsJsonAsync("/api/v1/todo/agent/settings", new
        {
            principalUserId = factory.SeedState.AdminUserId.ToString(),
            agentUserId = "agent-id",
            scopeType = "All",
            startTime = DateTime.UtcNow.AddHours(-1),
            endTime = DateTime.UtcNow.AddHours(2)
        });
        Assert.Equal(HttpStatusCode.OK, createAgentResponse.StatusCode);

        var listAgentResponse = await client.GetAsync($"/api/v1/todo/agent/settings?principalUserId={factory.SeedState.AdminUserId}");
        Assert.Equal(HttpStatusCode.OK, listAgentResponse.StatusCode);
        var listAgentJson = await listAgentResponse.ReadJsonAsync();
        Assert.Single(listAgentJson["data"]?.AsArray() ?? []);

        Assert.Contains(factory.FakeTodoCenterGateway.Calls, call => call == "transfer:1:u3001");
        Assert.Contains(factory.FakeTodoCenterGateway.Calls, call => call.StartsWith("batch:2:", StringComparison.Ordinal));
        Assert.Contains(factory.FakeTodoCenterGateway.Calls, call => call.StartsWith("batch-reject:2:", StringComparison.Ordinal));
        Assert.Contains(factory.FakeTodoCenterGateway.Calls, call => call == $"agent-create:{factory.SeedState.AdminUserId}:agent-id");
        Assert.Contains(factory.FakeTodoCenterGateway.Calls, call => call == $"agent-list:{factory.SeedState.AdminUserId}");
    }

    [Fact]
    public async Task ProcessProxyEndpoints_QueryAndDetail_Succeed()
    {
        await using var factory = new HrmsApiFactory();
        using var client = await factory.CreateAuthenticatedClientAsync();

        var definitionsResponse = await client.GetAsync("/api/v1/process/definitions");
        Assert.Equal(HttpStatusCode.OK, definitionsResponse.StatusCode);
        var definitionsJson = await definitionsResponse.ReadJsonAsync();
        Assert.Single(definitionsJson["data"]?.AsArray() ?? []);
        Assert.Equal("PAYROLL_APPROVAL", definitionsJson["data"]?[0]?["processCode"]?.GetValue<string>());

        var listResponse = await client.GetAsync("/api/v1/process/instances?processCode=PAYROLL_APPROVAL&status=Running&businessType=PayrollRun&keyword=2026-05");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var listJson = await listResponse.ReadJsonAsync();
        Assert.Single(listJson["data"]?.AsArray() ?? []);
        Assert.Equal("Running", listJson["data"]?[0]?["status"]?.GetValue<string>());

        var detailResponse = await client.GetAsync("/api/v1/process/instances/1001");
        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
        var detailJson = await detailResponse.ReadJsonAsync();
        Assert.Equal("PAYROLL_APPROVAL", detailJson["data"]?["processCode"]?.GetValue<string>());
        Assert.Equal(2, detailJson["data"]?["history"]?.AsArray().Count);

        Assert.Contains(factory.FakeWorkflowProcessCenterGateway.Calls, call => call == "definitions");
        Assert.Contains(factory.FakeWorkflowProcessCenterGateway.Calls, call => call.StartsWith("instances:PAYROLL_APPROVAL:Running:PayrollRun", StringComparison.Ordinal));
        Assert.Contains(factory.FakeWorkflowProcessCenterGateway.Calls, call => call == "instance:1001");
    }

    [Fact]
    public async Task ProcessProxyEndpoints_Start_Succeeds()
    {
        await using var factory = new HrmsApiFactory();
        using var client = await factory.CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/v1/process/start", new
        {
            processCode = "PAYROLL_APPROVAL",
            businessSystem = "HRMS",
            businessType = "PayrollRun",
            businessId = "RUN-2026-05",
            title = "2026-05 薪资审批申请",
            starterId = factory.SeedState.AdminUserId.ToString(),
            starterName = "管理员",
            requestId = "req-20260524-001",
            extData = "{\"yearMonth\":\"2026-05\"}"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.ReadJsonAsync();
        Assert.Equal("PAYROLL_APPROVAL", json["data"]?["processCode"]?.GetValue<string>());
        Assert.Equal("RUN-2026-05", json["data"]?["businessId"]?.GetValue<string>());
        Assert.Contains(
            factory.FakeWorkflowProcessCenterGateway.Calls,
            call => call == $"start:PAYROLL_APPROVAL:RUN-2026-05:{factory.SeedState.AdminUserId}");
    }

    [Fact]
    public async Task ProcessProxyEndpoints_Terminate_Succeeds()
    {
        await using var factory = new HrmsApiFactory();
        using var client = await factory.CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/v1/process/instances/1001/terminate", new
        {
            operatorId = factory.SeedState.AdminUserId.ToString(),
            operatorName = "管理员",
            comment = "运维终止"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.ReadJsonAsync();
        Assert.Equal("Terminated", json["data"]?["status"]?.GetValue<string>());
        Assert.Contains(factory.FakeWorkflowProcessCenterGateway.Calls, call => call.StartsWith("terminate:1001:", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ProcessProxyEndpoints_Reject_Succeeds()
    {
        await using var factory = new HrmsApiFactory();
        using var client = await factory.CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/v1/process/instances/1001/reject", new
        {
            operatorId = factory.SeedState.AdminUserId.ToString(),
            operatorName = "管理员",
            comment = "人工驳回"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.ReadJsonAsync();
        Assert.Equal("Rejected", json["data"]?["status"]?.GetValue<string>());
        Assert.Contains(factory.FakeWorkflowProcessCenterGateway.Calls, call => call.StartsWith("reject:1001:", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ProcessProxyEndpoints_RebuildCurrentTodo_Succeeds()
    {
        await using var factory = new HrmsApiFactory();
        using var client = await factory.CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/v1/process/instances/1001/rebuild-current-todo", new
        {
            operatorId = factory.SeedState.AdminUserId.ToString(),
            operatorName = "管理员",
            comment = "补发待办"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.ReadJsonAsync();
        Assert.Equal("Running", json["data"]?["instance"]?["status"]?.GetValue<string>());
        Assert.Equal("TD-REBUILD-0001", json["data"]?["task"]?["taskNo"]?.GetValue<string>());
        Assert.Contains(factory.FakeWorkflowProcessCenterGateway.Calls, call => call.StartsWith("rebuild:1001:", StringComparison.Ordinal));
    }
}
