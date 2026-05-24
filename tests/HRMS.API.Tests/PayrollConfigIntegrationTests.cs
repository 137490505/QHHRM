using System.Net;
using System.Net.Http.Json;

namespace HRMS.API.Tests;

public class PayrollConfigIntegrationTests
{
    [Fact]
    public async Task PayrollConfigEndpoints_ForMissingP0Configs_Workflow_Succeeds()
    {
        await using var factory = new HrmsApiFactory();
        using var client = await factory.CreateAuthenticatedClientAsync();

        var employeeTypeResponse = await client.PostAsJsonAsync("/api/v1/payroll/employee-types", new
        {
            typeCode = "HOURLY_TEST",
            typeName = "小时工测试",
            salaryMode = "Hourly",
            hasOvertime = true,
            hasMealSubsidy = true,
            hasNightSubsidy = false,
            hasPerformance = false,
            hasSocialSecurity = true,
            isActive = true,
            sortOrder = 1,
            remark = "integration-test"
        });

        Assert.Equal(HttpStatusCode.OK, employeeTypeResponse.StatusCode);
        var employeeTypeJson = await employeeTypeResponse.ReadJsonAsync();
        var employeeTypeId = employeeTypeJson["data"]?["id"]?.GetValue<Guid>() ?? Guid.Empty;
        Assert.NotEqual(Guid.Empty, employeeTypeId);

        var overtimeCreateResponse = await client.PostAsJsonAsync("/api/v1/payroll/overtime-rate-configs", new
        {
            configCode = "OT-WD-001",
            employeeTypeId,
            holidayType = "Workday",
            multiplier = 1.5m,
            effectiveStart = new DateTime(2026, 5, 1),
            effectiveEnd = new DateTime(2026, 12, 31),
            isActive = true,
            remark = "工作日加班"
        });

        Assert.Equal(HttpStatusCode.OK, overtimeCreateResponse.StatusCode);
        var overtimeCreateJson = await overtimeCreateResponse.ReadJsonAsync();
        var overtimeConfigId = overtimeCreateJson["data"]?["id"]?.GetValue<Guid>() ?? Guid.Empty;
        Assert.NotEqual(Guid.Empty, overtimeConfigId);

        var overtimeListResponse = await client.GetAsync($"/api/v1/payroll/overtime-rate-configs?employeeTypeId={employeeTypeId}");
        Assert.Equal(HttpStatusCode.OK, overtimeListResponse.StatusCode);

        var overtimeUpdateResponse = await client.PutAsJsonAsync($"/api/v1/payroll/overtime-rate-configs/{overtimeConfigId}", new
        {
            id = overtimeConfigId,
            configCode = "OT-WD-001",
            employeeTypeId,
            holidayType = "Workday",
            multiplier = 2.0m,
            effectiveStart = new DateTime(2026, 5, 1),
            effectiveEnd = new DateTime(2026, 12, 31),
            isActive = true,
            remark = "工作日加班更新"
        });
        Assert.Equal(HttpStatusCode.OK, overtimeUpdateResponse.StatusCode);

        var holidayCreateResponse = await client.PostAsJsonAsync("/api/v1/payroll/holiday-rules", new
        {
            holidayCode = "LABOR-2026",
            holidayName = "劳动节",
            holidayDate = new DateTime(2026, 5, 1),
            holidayType = "Holiday",
            overtimeMultiplier = 3.0m,
            isActive = true,
            remark = "法定节假日"
        });

        Assert.Equal(HttpStatusCode.OK, holidayCreateResponse.StatusCode);
        var holidayCreateJson = await holidayCreateResponse.ReadJsonAsync();
        var holidayRuleId = holidayCreateJson["data"]?["id"]?.GetValue<Guid>() ?? Guid.Empty;
        Assert.NotEqual(Guid.Empty, holidayRuleId);

        var holidayListResponse = await client.GetAsync("/api/v1/payroll/holiday-rules?startDate=2026-05-01&endDate=2026-05-31");
        Assert.Equal(HttpStatusCode.OK, holidayListResponse.StatusCode);

        var holidayUpdateResponse = await client.PutAsJsonAsync($"/api/v1/payroll/holiday-rules/{holidayRuleId}", new
        {
            id = holidayRuleId,
            holidayCode = "LABOR-2026",
            holidayName = "劳动节假期",
            holidayDate = new DateTime(2026, 5, 1),
            holidayType = "Holiday",
            overtimeMultiplier = 3.0m,
            isActive = true,
            remark = "节假日更新"
        });
        Assert.Equal(HttpStatusCode.OK, holidayUpdateResponse.StatusCode);

        var incomeTaxCreateResponse = await client.PostAsJsonAsync("/api/v1/payroll/income-tax-rules", new
        {
            ruleYear = 2026,
            levelNo = 1,
            minTaxableAmount = 0m,
            maxTaxableAmount = 3000m,
            taxRate = 0.03m,
            quickDeduction = 0m,
            thresholdAmount = 5000m,
            isActive = true,
            remark = "integration-test"
        });

        Assert.Equal(HttpStatusCode.OK, incomeTaxCreateResponse.StatusCode);
        var incomeTaxCreateJson = await incomeTaxCreateResponse.ReadJsonAsync();
        var incomeTaxRuleId = incomeTaxCreateJson["data"]?["id"]?.GetValue<Guid>() ?? Guid.Empty;
        Assert.NotEqual(Guid.Empty, incomeTaxRuleId);

        var incomeTaxListResponse = await client.GetAsync("/api/v1/payroll/income-tax-rules?ruleYear=2026");
        Assert.Equal(HttpStatusCode.OK, incomeTaxListResponse.StatusCode);
        var incomeTaxListJson = await incomeTaxListResponse.ReadJsonAsync();
        Assert.Single(incomeTaxListJson["data"]?.AsArray() ?? []);

        var incomeTaxUpdateResponse = await client.PutAsJsonAsync($"/api/v1/payroll/income-tax-rules/{incomeTaxRuleId}", new
        {
            id = incomeTaxRuleId,
            ruleYear = 2026,
            levelNo = 1,
            minTaxableAmount = 0m,
            maxTaxableAmount = 3600m,
            taxRate = 0.03m,
            quickDeduction = 0m,
            thresholdAmount = 5000m,
            isActive = true,
            remark = "integration-test-update"
        });
        Assert.Equal(HttpStatusCode.OK, incomeTaxUpdateResponse.StatusCode);
    }
}
