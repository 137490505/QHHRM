using System.Net;
using System.Net.Http.Json;

namespace HRMS.API.Tests;

public class PayrollRunIntegrationTests
{
    [Fact]
    public async Task PayrollRunEndpoints_ApproveAndPay_Workflow_Succeeds()
    {
        await using var factory = new HrmsApiFactory();
        using var client = await factory.CreateAuthenticatedClientAsync();
        var state = factory.SeedState;

        var employeeTypeId = await SetupPayrollDataAsync(client, state.InternalEmployeeId, "MANAGEMENT_TEST", "RULE-MGT-001", "OT-MGT-WD");

        var trialResponse = await client.PostAsJsonAsync("/api/v1/payroll/runs/trial", new
        {
            yearMonth = "2026-05",
            employeeIds = new[] { state.InternalEmployeeId }
        });
        Assert.Equal(HttpStatusCode.OK, trialResponse.StatusCode);
        var trialJson = await trialResponse.ReadJsonAsync();
        Assert.Equal("Trial", trialJson["data"]?["runType"]?.GetValue<string>());
        Assert.Single(trialJson["data"]?["payrolls"]?.AsArray() ?? []);

        var calculateResponse = await client.PostAsJsonAsync("/api/v1/payroll/runs/calculate", new
        {
            yearMonth = "2026-05",
            employeeIds = new[] { state.InternalEmployeeId }
        });
        Assert.Equal(HttpStatusCode.OK, calculateResponse.StatusCode);
        var calculateJson = await calculateResponse.ReadJsonAsync();
        var runId = calculateJson["data"]?["id"]?.GetValue<Guid>() ?? Guid.Empty;
        Assert.NotEqual(Guid.Empty, runId);
        Assert.Equal("Calculated", calculateJson["data"]?["status"]?.GetValue<string>());

        var payrollId = calculateJson["data"]?["payrolls"]?[0]?["payrollId"]?.GetValue<Guid>() ?? Guid.Empty;
        Assert.NotEqual(Guid.Empty, payrollId);
        var netSalary = calculateJson["data"]?["payrolls"]?[0]?["netSalary"]?.GetValue<decimal>() ?? 0m;
        Assert.True(netSalary > 0);

        var approveResponse = await client.PostAsJsonAsync($"/api/v1/payroll/runs/{runId}/approve", new
        {
            remark = "finance-approved"
        });
        Assert.Equal(HttpStatusCode.OK, approveResponse.StatusCode);
        var approveJson = await approveResponse.ReadJsonAsync();
        Assert.Equal("Approved", approveJson["data"]?["status"]?.GetValue<string>());
        Assert.False(string.IsNullOrWhiteSpace(approveJson["data"]?["approvedBy"]?.GetValue<string>()));

        var payResponse = await client.PostAsJsonAsync($"/api/v1/payroll/runs/{runId}/pay", new
        {
            remark = "bank-paid"
        });
        Assert.Equal(HttpStatusCode.OK, payResponse.StatusCode);
        var payJson = await payResponse.ReadJsonAsync();
        Assert.Equal("Paid", payJson["data"]?["status"]?.GetValue<string>());

        var listResponse = await client.GetAsync("/api/v1/payroll/runs?yearMonth=2026-05");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var listJson = await listResponse.ReadJsonAsync();
        Assert.Single(listJson["data"]?.AsArray() ?? []);

        var detailResponse = await client.GetAsync($"/api/v1/payroll/runs/{runId}");
        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);

        var employeePayrollResponse = await client.GetAsync($"/api/v1/payroll/runs/{runId}/employees/{state.InternalEmployeeId}");
        Assert.Equal(HttpStatusCode.OK, employeePayrollResponse.StatusCode);
        var employeePayrollJson = await employeePayrollResponse.ReadJsonAsync();
        Assert.NotEmpty(employeePayrollJson["data"]?["details"]?.AsArray() ?? []);
        Assert.Equal("Paid", employeePayrollJson["data"]?["status"]?.GetValue<string>());
        Assert.NotNull(employeePayrollJson["data"]?["paidAt"]);
    }

    [Fact]
    public async Task PayrollRunEndpoints_Rollback_Workflow_Succeeds()
    {
        await using var factory = new HrmsApiFactory();
        using var client = await factory.CreateAuthenticatedClientAsync();
        var state = factory.SeedState;

        await SetupPayrollDataAsync(client, state.InternalEmployeeId, "MANAGEMENT_TEST_ROLLBACK", "RULE-MGT-002", "OT-MGT-WD-2");

        var calculateResponse = await client.PostAsJsonAsync("/api/v1/payroll/runs/calculate", new
        {
            yearMonth = "2026-05",
            employeeIds = new[] { state.InternalEmployeeId }
        });
        Assert.Equal(HttpStatusCode.OK, calculateResponse.StatusCode);
        var calculateJson = await calculateResponse.ReadJsonAsync();
        var runId = calculateJson["data"]?["id"]?.GetValue<Guid>() ?? Guid.Empty;
        Assert.NotEqual(Guid.Empty, runId);

        var rollbackResponse = await client.PostAsync($"/api/v1/payroll/runs/{runId}/rollback?remark=integration-test", null);
        Assert.Equal(HttpStatusCode.OK, rollbackResponse.StatusCode);
        var rollbackJson = await rollbackResponse.ReadJsonAsync();
        Assert.Equal("RolledBack", rollbackJson["data"]?["status"]?.GetValue<string>());
    }

    [Fact]
    public async Task PayrollRunEndpoints_SubmitApproval_StartsProcess_Workflow_Succeeds()
    {
        await using var factory = new HrmsApiFactory();
        using var client = await factory.CreateAuthenticatedClientAsync();
        var state = factory.SeedState;

        await SetupPayrollDataAsync(client, state.InternalEmployeeId, "MANAGEMENT_TEST_SUBMIT", "RULE-MGT-003", "OT-MGT-WD-3");

        var calculateResponse = await client.PostAsJsonAsync("/api/v1/payroll/runs/calculate", new
        {
            yearMonth = "2026-05",
            employeeIds = new[] { state.InternalEmployeeId }
        });
        Assert.Equal(HttpStatusCode.OK, calculateResponse.StatusCode);
        var calculateJson = await calculateResponse.ReadJsonAsync();
        var runId = calculateJson["data"]?["id"]?.GetValue<Guid>() ?? Guid.Empty;
        Assert.NotEqual(Guid.Empty, runId);

        var submitResponse = await client.PostAsJsonAsync($"/api/v1/payroll/runs/{runId}/submit-approval", new
        {
            remark = "submit-to-process-center"
        });

        Assert.Equal(HttpStatusCode.OK, submitResponse.StatusCode);
        var submitJson = await submitResponse.ReadJsonAsync();
        Assert.Equal("PROC-0001", submitJson["data"]?["processInstanceId"]?.GetValue<string>());
        Assert.Equal("PAYROLL_APPROVAL", submitJson["data"]?["processCode"]?.GetValue<string>());
        Assert.Equal(runId, submitJson["data"]?["run"]?["id"]?.GetValue<Guid>());
        Assert.Equal("Calculated", submitJson["data"]?["run"]?["status"]?.GetValue<string>());
        Assert.Equal("PROC-0001", submitJson["data"]?["run"]?["approvalProcessInstanceId"]?.GetValue<string>());
        Assert.NotNull(submitJson["data"]?["run"]?["approvalSubmittedAt"]);

        Assert.Single(factory.FakeProcessCenterClient.Calls);
        Assert.Equal(runId, factory.FakeProcessCenterClient.Calls[0].RunId);
        Assert.Equal("2026-05", factory.FakeProcessCenterClient.Calls[0].SourcePayload.YearMonth);
        Assert.Equal("Calculated", factory.FakeProcessCenterClient.Calls[0].SourcePayload.Status);
        Assert.Equal(1, factory.FakeProcessCenterClient.Calls[0].SourcePayload.PayrollCount);
        Assert.Equal("submit-to-process-center", factory.FakeProcessCenterClient.Calls[0].SourcePayload.Remark);
        Assert.True(factory.FakeProcessCenterClient.Calls[0].SourcePayload.TotalNetSalary > 0);
        Assert.Equal(factory.SeedState.AdminUserId.ToString(), factory.FakeProcessCenterClient.Calls[0].SourcePayload.StarterId);

        var detailResponse = await client.GetAsync($"/api/v1/payroll/runs/{runId}");
        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
        var detailJson = await detailResponse.ReadJsonAsync();
        Assert.Equal("PROC-0001", detailJson["data"]?["approvalProcessInstanceId"]?.GetValue<string>());
    }

    [Fact]
    public async Task PayrollRunEndpoints_WorkflowApprovedCallback_ApprovesRun()
    {
        await using var factory = new HrmsApiFactory();
        using var client = await factory.CreateAuthenticatedClientAsync();
        var state = factory.SeedState;

        await SetupPayrollDataAsync(client, state.InternalEmployeeId, "MANAGEMENT_TEST_CALLBACK_APPROVE", "RULE-MGT-004", "OT-MGT-WD-4");

        var calculateResponse = await client.PostAsJsonAsync("/api/v1/payroll/runs/calculate", new
        {
            yearMonth = "2026-05",
            employeeIds = new[] { state.InternalEmployeeId }
        });
        Assert.Equal(HttpStatusCode.OK, calculateResponse.StatusCode);
        var calculateJson = await calculateResponse.ReadJsonAsync();
        var runId = calculateJson["data"]?["id"]?.GetValue<Guid>() ?? Guid.Empty;
        Assert.NotEqual(Guid.Empty, runId);

        var submitResponse = await client.PostAsJsonAsync($"/api/v1/payroll/runs/{runId}/submit-approval", new
        {
            remark = "submit-to-process-center"
        });
        Assert.Equal(HttpStatusCode.OK, submitResponse.StatusCode);

        var callbackResponse = await TestApiHelper.PostSignedJsonAsync(
            client,
            $"/api/v1/payroll/runs/{runId}/workflow-callback/approved",
            new
            {
                processInstanceId = "PROC-0001",
                operatorId = "u1002",
                operatorName = "财务经理",
                comment = "审批通过"
            });

        Assert.Equal(HttpStatusCode.OK, callbackResponse.StatusCode);
        var callbackJson = await callbackResponse.ReadJsonAsync();
        Assert.Equal("Approved", callbackJson["data"]?["status"]?.GetValue<string>());
        Assert.Equal("u1002", callbackJson["data"]?["approvedBy"]?.GetValue<string>());
        Assert.NotNull(callbackJson["data"]?["approvedAt"]);
        Assert.Equal("Approved", callbackJson["data"]?["payrolls"]?[0]?["status"]?.GetValue<string>());
    }

    [Fact]
    public async Task PayrollRunEndpoints_WorkflowRejectedCallback_MarksRunRejected()
    {
        await using var factory = new HrmsApiFactory();
        using var client = await factory.CreateAuthenticatedClientAsync();
        var state = factory.SeedState;

        await SetupPayrollDataAsync(client, state.InternalEmployeeId, "MANAGEMENT_TEST_CALLBACK_REJECT", "RULE-MGT-005", "OT-MGT-WD-5");

        var calculateResponse = await client.PostAsJsonAsync("/api/v1/payroll/runs/calculate", new
        {
            yearMonth = "2026-05",
            employeeIds = new[] { state.InternalEmployeeId }
        });
        Assert.Equal(HttpStatusCode.OK, calculateResponse.StatusCode);
        var calculateJson = await calculateResponse.ReadJsonAsync();
        var runId = calculateJson["data"]?["id"]?.GetValue<Guid>() ?? Guid.Empty;
        Assert.NotEqual(Guid.Empty, runId);

        var submitResponse = await client.PostAsJsonAsync($"/api/v1/payroll/runs/{runId}/submit-approval", new
        {
            remark = "submit-to-process-center"
        });
        Assert.Equal(HttpStatusCode.OK, submitResponse.StatusCode);

        var callbackResponse = await TestApiHelper.PostSignedJsonAsync(
            client,
            $"/api/v1/payroll/runs/{runId}/workflow-callback/rejected",
            new
            {
                processInstanceId = "PROC-0001",
                operatorId = "u1001",
                operatorName = "部门经理",
                comment = "金额异常，驳回重算"
            });

        Assert.Equal(HttpStatusCode.OK, callbackResponse.StatusCode);
        var callbackJson = await callbackResponse.ReadJsonAsync();
        Assert.Equal("ApprovalRejected", callbackJson["data"]?["status"]?.GetValue<string>());
        Assert.Equal("ApprovalRejected", callbackJson["data"]?["payrolls"]?[0]?["status"]?.GetValue<string>());
        Assert.Contains("ApprovalReject", callbackJson["data"]?["remark"]?.GetValue<string>() ?? string.Empty);
    }

    private static async Task<Guid> SetupPayrollDataAsync(HttpClient client, Guid employeeId, string employeeTypeCode, string ruleCode, string overtimeCode)
    {
        var employeeTypeResponse = await client.PostAsJsonAsync("/api/v1/payroll/employee-types", new
        {
            typeCode = employeeTypeCode,
            typeName = $"{employeeTypeCode}-名称",
            salaryMode = "Management",
            hasOvertime = true,
            hasMealSubsidy = true,
            hasNightSubsidy = true,
            hasPerformance = true,
            hasSocialSecurity = true,
            isActive = true,
            sortOrder = 1
        });
        Assert.Equal(HttpStatusCode.OK, employeeTypeResponse.StatusCode);
        var employeeTypeId = (await employeeTypeResponse.ReadJsonAsync())["data"]?["id"]?.GetValue<Guid>() ?? Guid.Empty;
        Assert.NotEqual(Guid.Empty, employeeTypeId);

        var salaryRuleResponse = await client.PostAsJsonAsync("/api/v1/payroll/salary-rules", new
        {
            ruleCode,
            ruleName = $"{ruleCode}-名称",
            employeeTypeId,
            effectiveStart = new DateTime(2026, 5, 1),
            fixedSalary = 8000m,
            hourlyRate = 50m,
            mealSubsidyPerDay = 20m,
            nightSubsidyPerDay = 15m,
            performanceBase = 500m,
            defaultOvertimeMultiplier = 1.5m,
            isActive = true
        });
        Assert.Equal(HttpStatusCode.OK, salaryRuleResponse.StatusCode);

        var overtimeConfigResponse = await client.PostAsJsonAsync("/api/v1/payroll/overtime-rate-configs", new
        {
            configCode = overtimeCode,
            employeeTypeId,
            holidayType = "Workday",
            multiplier = 2.0m,
            effectiveStart = new DateTime(2026, 5, 1),
            isActive = true
        });
        Assert.Equal(HttpStatusCode.OK, overtimeConfigResponse.StatusCode);

        var profileResponse = await client.PostAsJsonAsync("/api/v1/payroll/profiles", new
        {
            employeeId,
            employeeTypeId,
            payrollStatus = "Active",
            joinPayrollDate = new DateTime(2026, 5, 1)
        });
        Assert.Equal(HttpStatusCode.OK, profileResponse.StatusCode);

        var attendanceResponse = await client.PostAsJsonAsync("/api/v1/payroll/attendance", new
        {
            employeeId,
            workDate = new DateTime(2026, 5, 10),
            normalHours = 8m,
            overtimeHours = 2m,
            overtimeType = "Workday",
            pieceworkQty = 0m,
            isNightShift = true,
            attendanceSource = "Manual",
            status = "Approved"
        });
        Assert.Equal(HttpStatusCode.OK, attendanceResponse.StatusCode);

        var adjustmentResponse = await client.PostAsJsonAsync("/api/v1/payroll/adjustments", new
        {
            employeeId,
            yearMonth = "2026-05",
            adjustmentType = "SocialSecurityPersonal",
            amount = 200m,
            sourceType = "Manual"
        });
        Assert.Equal(HttpStatusCode.OK, adjustmentResponse.StatusCode);

        return employeeTypeId;
    }
}
