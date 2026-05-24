using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace HRMS.API.Tests;

public class AuthControllerIntegrationTests
{
    [Fact]
    public async Task Login_Me_Logout_Workflow_Succeeds()
    {
        await using var factory = new HrmsApiFactory();
        await factory.ResetAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            username = "admin",
            password = "123456"
        });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var loginJson = await loginResponse.ReadJsonAsync();
        var token = loginJson["data"]?["token"]?.GetValue<string>();
        Assert.False(string.IsNullOrWhiteSpace(token));

        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var meResponse = await client.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);
        var meJson = await meResponse.ReadJsonAsync();
        Assert.Equal("admin", meJson["data"]?["userInfo"]?["username"]?.GetValue<string>());

        var logoutResponse = await client.PostAsync("/api/v1/auth/logout", null);
        Assert.Equal(HttpStatusCode.OK, logoutResponse.StatusCode);

        var afterLogout = await client.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, afterLogout.StatusCode);
    }

    [Fact]
    public async Task LoginOptions_ReturnsCaptchaConfig()
    {
        await using var factory = new HrmsApiFactory();
        await factory.ResetAsync();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/auth/login-options");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.ReadJsonAsync();
        Assert.False(json["data"]?["captchaEnabled"]?.GetValue<bool>() ?? true);
    }
}

public class CaptchaControllerIntegrationTests
{
    [Fact]
    public async Task Get_ReturnsSvgAndSetsSessionCookie()
    {
        await using var factory = new HrmsApiFactory();
        await factory.ResetAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });

        var response = await client.GetAsync("/api/v1/captcha");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/svg+xml", response.Content.Headers.ContentType?.MediaType);

        var svg = await response.Content.ReadAsStringAsync();
        Assert.Contains("<svg", svg);
        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var cookies));
        Assert.NotEmpty(cookies);
    }
}

public class OrgUnitsControllerIntegrationTests
{
    [Fact]
    public async Task OrgUnitEndpoints_Workflow_Succeeds()
    {
        await using var factory = new HrmsApiFactory();
        using var client = await factory.CreateAuthenticatedClientAsync();
        var state = factory.SeedState;

        var allResponse = await client.GetAsync("/api/v1/org-units");
        Assert.Equal(HttpStatusCode.OK, allResponse.StatusCode);

        var treeResponse = await client.GetAsync("/api/v1/org-units/tree");
        Assert.Equal(HttpStatusCode.OK, treeResponse.StatusCode);

        var childrenResponse = await client.GetAsync($"/api/v1/org-units/{state.BranchId}/children");
        Assert.Equal(HttpStatusCode.OK, childrenResponse.StatusCode);

        var getByIdResponse = await client.GetAsync($"/api/v1/org-units/{state.DepartmentId}");
        Assert.Equal(HttpStatusCode.OK, getByIdResponse.StatusCode);

        var getByCodeResponse = await client.GetAsync("/api/v1/org-units/code/NJ-HR");
        Assert.Equal(HttpStatusCode.OK, getByCodeResponse.StatusCode);

        var createResponse = await client.PostAsJsonAsync("/api/v1/org-units", new
        {
            code = "NJ-HR-L1",
            name = "一线班组",
            level = 3,
            parentId = state.DepartmentId,
            managerId = (string?)null
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var createJson = await createResponse.ReadJsonAsync();
        var createdId = createJson["data"]?["id"]?.GetValue<Guid>() ?? Guid.Empty;
        Assert.NotEqual(Guid.Empty, createdId);

        var updateResponse = await client.PutAsJsonAsync("/api/v1/org-units", new
        {
            id = createdId,
            code = "NJ-HR-L1A",
            name = "一线班组A",
            level = 4,
            parentId = state.DepartmentId,
            managerId = (string?)null,
            isActive = true
        });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var batchDisableResponse = await client.PostAsJsonAsync("/api/v1/org-units/batch-disable", new[] { createdId });
        Assert.Equal(HttpStatusCode.OK, batchDisableResponse.StatusCode);

        var batchEnableResponse = await client.PostAsJsonAsync("/api/v1/org-units/batch-enable", new[] { createdId });
        Assert.Equal(HttpStatusCode.OK, batchEnableResponse.StatusCode);

        var batchUpdateResponse = await client.PostAsJsonAsync("/api/v1/org-units/batch-update", new
        {
            ids = new[] { createdId },
            level = 3,
            isActive = true
        });
        Assert.Equal(HttpStatusCode.OK, batchUpdateResponse.StatusCode);

        var toggleStatusResponse = await client.PostAsync($"/api/v1/org-units/{createdId}/toggle-status", null);
        Assert.Equal(HttpStatusCode.OK, toggleStatusResponse.StatusCode);

        var deleteResponse = await client.DeleteAsync($"/api/v1/org-units/{createdId}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
    }
}

public class EmployeesControllerIntegrationTests
{
    [Fact]
    public async Task EmployeeEndpoints_Workflow_Succeeds()
    {
        await using var factory = new HrmsApiFactory();
        using var client = await factory.CreateAuthenticatedClientAsync();
        var state = factory.SeedState;

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/employees")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/v1/employees/{state.InternalEmployeeId}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/employees/no/10001")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/v1/employees/org-unit/{state.DepartmentId}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/v1/employees/third-party/{state.SupplierId}")).StatusCode);

        var createResponse = await client.PostAsJsonAsync("/api/v1/employees", new
        {
            employeeNo = "30001",
            name = "王五",
            idCard = "110105199003031236",
            phone = "13800000003",
            email = "wangwu@example.com",
            employeeType = 0,
            salaryMode = 0,
            orgUnitId = state.DepartmentId,
            thirdPartyCompanyId = (Guid?)null,
            jobTitle = "专员",
            level = "P1",
            tags = new[] { "试用期" },
            hourlyRate = 28,
            monthlySalary = (decimal?)null,
            pieceRatePrice = (decimal?)null,
            contractStartDate = new DateTime(2026, 1, 1),
            contractEndDate = new DateTime(2026, 12, 31)
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var createJson = await createResponse.ReadJsonAsync();
        var employeeId = createJson["data"]?["id"]?.GetValue<Guid>() ?? Guid.Empty;
        Assert.NotEqual(Guid.Empty, employeeId);

        var updateResponse = await client.PutAsJsonAsync("/api/v1/employees", new
        {
            id = employeeId,
            name = "王五-更新",
            idCard = "110105199003031236",
            phone = "13800000013",
            email = "wangwu2@example.com",
            orgUnitId = state.DepartmentId,
            thirdPartyCompanyId = (Guid?)null,
            jobTitle = "高级专员",
            level = "P2",
            tags = new[] { "正式员工" },
            hourlyRate = 30,
            monthlySalary = (decimal?)null,
            pieceRatePrice = (decimal?)null
        });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var toggleDismissResponse = await client.PostAsync($"/api/v1/employees/{employeeId}/toggle-dismiss", null);
        Assert.Equal(HttpStatusCode.OK, toggleDismissResponse.StatusCode);

        var batchDismissResponse = await client.PostAsJsonAsync("/api/v1/employees/batch-dismiss", new
        {
            ids = new[] { employeeId },
            dismissDate = new DateTime(2026, 5, 1),
            reason = "测试离职"
        });
        Assert.Equal(HttpStatusCode.OK, batchDismissResponse.StatusCode);

        var deleteResponse = await client.DeleteAsync($"/api/v1/employees/{employeeId}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
    }
}

public class CustomersControllerIntegrationTests
{
    [Fact]
    public async Task CustomerEndpoints_Workflow_Succeeds()
    {
        await using var factory = new HrmsApiFactory();
        using var client = await factory.CreateAuthenticatedClientAsync();
        var state = factory.SeedState;

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/customers")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/v1/customers/{state.CustomerId}")).StatusCode);

        var createResponse = await client.PostAsJsonAsync("/api/v1/customers", new
        {
            code = "CUS-NEW-01",
            name = "新增客户",
            shortName = "新增",
            contactPerson = "客户对接",
            phone = "13812345678",
            email = "customer-new@example.com",
            taxNo = "91320000CUSNEW001",
            address = "南京市客户路 1 号",
            invoiceTitle = "新增客户有限公司",
            remark = "接口测试"
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var createJson = await createResponse.ReadJsonAsync();
        var customerId = createJson["data"]?["id"]?.GetValue<Guid>() ?? Guid.Empty;
        Assert.NotEqual(Guid.Empty, customerId);

        var updateResponse = await client.PutAsJsonAsync("/api/v1/customers", new
        {
            id = customerId,
            code = "CUS-NEW-01",
            name = "新增客户-更新",
            shortName = "新增更新",
            contactPerson = "客户对接2",
            phone = "13812345679",
            email = "customer-new2@example.com",
            taxNo = "91320000CUSNEW002",
            address = "南京市客户路 2 号",
            invoiceTitle = "新增客户有限公司-更新",
            remark = "接口测试更新",
            isActive = true
        });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var deleteResponse = await client.DeleteAsync($"/api/v1/customers/{customerId}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
    }
}

public class SuppliersControllerIntegrationTests
{
    [Fact]
    public async Task SupplierEndpoints_Workflow_Succeeds()
    {
        await using var factory = new HrmsApiFactory();
        using var client = await factory.CreateAuthenticatedClientAsync();
        var state = factory.SeedState;

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/suppliers")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/v1/suppliers/{state.SupplierId}")).StatusCode);

        var createResponse = await client.PostAsJsonAsync("/api/v1/suppliers", new
        {
            code = "SUP-NEW-01",
            name = "新增供应商",
            contactPerson = "供应商对接",
            phone = "13822345678",
            email = "supplier-new@example.com",
            taxNo = "91320000SUPNEW001",
            address = "苏州市供应商路 1 号",
            bankName = "招商银行",
            bankAccount = "6225888888888888888",
            paymentTermDays = 45,
            remark = "接口测试"
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var createJson = await createResponse.ReadJsonAsync();
        var supplierId = createJson["data"]?["id"]?.GetValue<Guid>() ?? Guid.Empty;
        Assert.NotEqual(Guid.Empty, supplierId);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/v1/employees/third-party/{state.SupplierId}")).StatusCode);

        var updateResponse = await client.PutAsJsonAsync("/api/v1/suppliers", new
        {
            id = supplierId,
            code = "SUP-NEW-01",
            name = "新增供应商-更新",
            contactPerson = "供应商对接2",
            phone = "13822345679",
            email = "supplier-new2@example.com",
            taxNo = "91320000SUPNEW002",
            address = "苏州市供应商路 2 号",
            bankName = "工商银行",
            bankAccount = "6225999999999999999",
            paymentTermDays = 30,
            remark = "接口测试更新",
            isActive = true
        });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var deleteResponse = await client.DeleteAsync($"/api/v1/suppliers/{supplierId}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
    }
}

public class SettingsControllerIntegrationTests
{
    [Fact]
    public async Task SettingsEndpoints_Workflow_Succeeds()
    {
        await using var factory = new HrmsApiFactory();
        using var client = await factory.CreateAuthenticatedClientAsync();

        var getSecurity = await client.GetAsync("/api/v1/settings/login-security");
        Assert.Equal(HttpStatusCode.OK, getSecurity.StatusCode);

        var updateSecurity = await client.PutAsJsonAsync("/api/v1/settings/login-security", new
        {
            captchaEnabled = true
        });
        Assert.Equal(HttpStatusCode.OK, updateSecurity.StatusCode);

        var getOptions = await client.GetAsync("/api/v1/settings/select-options/employeeType");
        Assert.Equal(HttpStatusCode.OK, getOptions.StatusCode);
        var getOptionsJson = await getOptions.ReadJsonAsync();
        Assert.Equal(2, getOptionsJson["data"]?.AsArray().Count);

        var updateOptions = await client.PutAsJsonAsync("/api/v1/settings/select-options/employeeTag", new
        {
            options = new[]
            {
                new { value = "核心员工", label = "核心员工", sortOrder = 1 },
                new { value = "外派", label = "外派", sortOrder = 2 }
            }
        });
        Assert.Equal(HttpStatusCode.OK, updateOptions.StatusCode);
        var updateOptionsJson = await updateOptions.ReadJsonAsync();
        Assert.Equal("核心员工", updateOptionsJson["data"]?[0]?["label"]?.GetValue<string>());
    }
}

public class AccessControllerIntegrationTests
{
    [Fact]
    public async Task MenuEndpoints_Workflow_Succeeds()
    {
        await using var factory = new HrmsApiFactory();
        using var client = await factory.CreateAuthenticatedClientAsync();
        var state = factory.SeedState;

        var treeResponse = await client.GetAsync("/api/v1/access/menus/tree");
        Assert.Equal(HttpStatusCode.OK, treeResponse.StatusCode);

        var createResponse = await client.PostAsJsonAsync("/api/v1/access/menus", new
        {
            parentId = state.MenuIds["access-module"],
            menuKey = "test-menu",
            menuName = "测试菜单",
            menuType = "page",
            sortOrder = 760,
            routePath = "/test-menu",
            componentPath = "views/TestMenu.vue",
            icon = "ApiOutlined",
            isVisible = true,
            isActive = true,
            permissionCode = "page.test.menu"
        });

        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);
        var createJson = await createResponse.ReadJsonAsync();
        var menuId = createJson["data"]?["id"]?.GetValue<Guid>() ?? Guid.Empty;
        Assert.NotEqual(Guid.Empty, menuId);

        var updateResponse = await client.PutAsJsonAsync("/api/v1/access/menus", new
        {
            id = menuId,
            parentId = state.MenuIds["access-module"],
            menuKey = "test-menu-updated",
            menuName = "测试菜单更新",
            menuType = "page",
            sortOrder = 780,
            routePath = "/test-menu-updated",
            componentPath = "views/TestMenuUpdated.vue",
            icon = "AppstoreOutlined",
            isVisible = true,
            isActive = true,
            permissionCode = "page.test.menu.updated"
        });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var deleteResponse = await client.DeleteAsync($"/api/v1/access/menus/{menuId}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task RoleEndpoints_Workflow_Succeeds()
    {
        await using var factory = new HrmsApiFactory();
        using var client = await factory.CreateAuthenticatedClientAsync();
        var state = factory.SeedState;

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/access/roles")).StatusCode);

        var createResponse = await client.PostAsJsonAsync("/api/v1/access/roles", new
        {
            roleCode = "TEST_ROLE",
            roleName = "测试角色",
            description = "角色接口测试",
            isActive = true
        });

        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);
        var createJson = await createResponse.ReadJsonAsync();
        var roleId = createJson["data"]?["id"]?.GetValue<Guid>() ?? Guid.Empty;
        Assert.NotEqual(Guid.Empty, roleId);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/v1/access/roles/{roleId}")).StatusCode);

        var assignResponse = await client.PutAsJsonAsync($"/api/v1/access/roles/{roleId}/permissions", new
        {
            menuIds = new[] { state.MenuIds["org-list"], state.MenuIds["employee-list"] }
        });
        Assert.Equal(HttpStatusCode.OK, assignResponse.StatusCode);

        var copyResponse = await client.PutAsJsonAsync($"/api/v1/access/roles/{roleId}/permissions/copy", new
        {
            sourceId = state.AdminRoleId
        });
        Assert.Equal(HttpStatusCode.OK, copyResponse.StatusCode);

        var updateResponse = await client.PutAsJsonAsync("/api/v1/access/roles", new
        {
            id = roleId,
            roleCode = "TEST_ROLE_2",
            roleName = "测试角色2",
            description = "更新后的角色",
            isActive = true
        });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var deleteResponse = await client.DeleteAsync($"/api/v1/access/roles/{roleId}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task PostAndUserEndpoints_Workflow_Succeeds()
    {
        await using var factory = new HrmsApiFactory();
        using var client = await factory.CreateAuthenticatedClientAsync();
        var state = factory.SeedState;

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/access/posts")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/access/users")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/access/options")).StatusCode);

        var createPost = await client.PostAsJsonAsync("/api/v1/access/posts", new
        {
            postCode = "TEST_POST",
            postName = "测试岗位",
            description = "岗位接口测试",
            isActive = true
        });
        Assert.Equal(HttpStatusCode.OK, createPost.StatusCode);
        var createPostJson = await createPost.ReadJsonAsync();
        var postId = createPostJson["data"]?["id"]?.GetValue<Guid>() ?? Guid.Empty;
        Assert.NotEqual(Guid.Empty, postId);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/v1/access/posts/{postId}")).StatusCode);

        var assignPostPermissions = await client.PutAsJsonAsync($"/api/v1/access/posts/{postId}/permissions", new
        {
            menuIds = new[] { state.MenuIds["settings-page"], state.MenuIds["config-page"] }
        });
        Assert.Equal(HttpStatusCode.OK, assignPostPermissions.StatusCode);

        var copyPostPermissions = await client.PutAsJsonAsync($"/api/v1/access/posts/{postId}/permissions/copy", new
        {
            sourceId = state.AdminPostId
        });
        Assert.Equal(HttpStatusCode.OK, copyPostPermissions.StatusCode);

        var updatePost = await client.PutAsJsonAsync("/api/v1/access/posts", new
        {
            id = postId,
            postCode = "TEST_POST_2",
            postName = "测试岗位2",
            description = "岗位更新",
            isActive = true
        });
        Assert.Equal(HttpStatusCode.OK, updatePost.StatusCode);

        var createUser = await client.PostAsJsonAsync("/api/v1/access/users", new
        {
            username = "tester",
            password = "123456",
            name = "测试用户",
            phone = "13800000088",
            email = "tester@example.com",
            employeeId = (Guid?)null,
            postId,
            isActive = true,
            isAdmin = false,
            roleIds = new[] { state.AdminRoleId }
        });
        Assert.Equal(HttpStatusCode.OK, createUser.StatusCode);
        var createUserJson = await createUser.ReadJsonAsync();
        var userId = createUserJson["data"]?["id"]?.GetValue<Guid>() ?? Guid.Empty;
        Assert.NotEqual(Guid.Empty, userId);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/v1/access/users/{userId}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/v1/access/users/{userId}/permissions")).StatusCode);

        var updateUser = await client.PutAsJsonAsync("/api/v1/access/users", new
        {
            id = userId,
            username = "tester2",
            name = "测试用户2",
            password = "654321",
            phone = "13800000089",
            email = "tester2@example.com",
            employeeId = (Guid?)null,
            postId,
            isActive = true,
            isAdmin = false,
            roleIds = new[] { state.AdminRoleId }
        });
        Assert.Equal(HttpStatusCode.OK, updateUser.StatusCode);

        var deleteUser = await client.DeleteAsync($"/api/v1/access/users/{userId}");
        Assert.Equal(HttpStatusCode.OK, deleteUser.StatusCode);

        var deletePost = await client.DeleteAsync($"/api/v1/access/posts/{postId}");
        Assert.Equal(HttpStatusCode.OK, deletePost.StatusCode);
    }
}

public class TimesheetsControllerIntegrationTests
{
    [Fact]
    public async Task TimesheetEndpoints_Workflow_Succeeds()
    {
        await using var factory = new HrmsApiFactory();
        using var client = await factory.CreateAuthenticatedClientAsync();
        var state = factory.SeedState;
        var date = new DateTime(2026, 5, 10);

        var createResponse = await client.PostAsJsonAsync("/api/v1/timesheets", new
        {
            employeeId = state.InternalEmployeeId,
            date,
            actualOrgUnitId = state.DepartmentId,
            workingHours = 10,
            shiftType = 0,
            remark = "正常加班"
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var createJson = await createResponse.ReadJsonAsync();
        var timesheetId = createJson["data"]?["id"]?.GetValue<Guid>() ?? Guid.Empty;
        Assert.NotEqual(Guid.Empty, timesheetId);
        Assert.Equal(2m, createJson["data"]?["overtimeHours"]?.GetValue<decimal>());

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/timesheets")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/v1/timesheets/{timesheetId}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/v1/timesheets/employee/{state.InternalEmployeeId}?year=2026&month=5")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/v1/timesheets/org-unit/{state.DepartmentId}?startDate=2026-05-01&endDate=2026-05-31")).StatusCode);

        var importResponse = await client.PostAsJsonAsync("/api/v1/timesheets/import", new[]
        {
            new
            {
                employeeNo = "20001",
                date = new DateTime(2026, 5, 11),
                actualOrgUnitCode = "NJ-HR",
                workingHours = 9,
                shiftType = 0,
                remark = "批量导入"
            }
        });
        Assert.Equal(HttpStatusCode.OK, importResponse.StatusCode);

        var approveResponse = await client.PostAsync($"/api/v1/timesheets/{timesheetId}/approve?approverId={state.AdminUserId}", null);
        Assert.Equal(HttpStatusCode.OK, approveResponse.StatusCode);
    }
}

public class SalaryControllerIntegrationTests
{
    [Fact]
    public async Task SalaryEndpoints_CalculateAndQuery_Succeed()
    {
        await using var factory = new HrmsApiFactory();
        using var client = await factory.CreateAuthenticatedClientAsync();
        var state = factory.SeedState;

        await TestApiHelper.CreateTimesheetAndApproveAsync(
            client,
            state.InternalEmployeeId,
            state.DepartmentId,
            state.AdminUserId,
            new DateTime(2026, 5, 12),
            10);

        var calculateResponse = await client.PostAsJsonAsync("/api/v1/salary/calculate", new
        {
            year = 2026,
            month = 5,
            employeeIds = new[] { state.InternalEmployeeId },
            orgUnitIds = new[] { state.DepartmentId }
        });

        Assert.Equal(HttpStatusCode.OK, calculateResponse.StatusCode);
        var calculateJson = await calculateResponse.ReadJsonAsync();
        Assert.Single(calculateJson["data"]?.AsArray() ?? []);

        var salaryId = calculateJson["data"]?[0]?["id"]?.GetValue<Guid>() ?? Guid.Empty;
        Assert.NotEqual(Guid.Empty, salaryId);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/salary")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/v1/salary/{salaryId}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/v1/salary/employee/{state.InternalEmployeeId}?year=2026&month=5")).StatusCode);
    }
}

public class ThirdPartyBillsControllerIntegrationTests
{
    [Fact]
    public async Task ThirdPartyBillEndpoints_GenerateAndQuery_Succeed()
    {
        await using var factory = new HrmsApiFactory();
        using var client = await factory.CreateAuthenticatedClientAsync();
        var state = factory.SeedState;

        await TestApiHelper.CreateTimesheetAndApproveAsync(
            client,
            state.ThirdPartyEmployeeId,
            state.DepartmentId,
            state.AdminUserId,
            new DateTime(2026, 5, 15),
            8);

        var generateResponse = await client.PostAsJsonAsync("/api/v1/third-party-bills/generate", new
        {
            clientId = state.DepartmentId,
            year = 2026,
            month = 5,
            managementFeeType = "Fixed",
            managementFeeRate = 100,
            otherFees = 20
        });

        Assert.Equal(HttpStatusCode.OK, generateResponse.StatusCode);
        var generateJson = await generateResponse.ReadJsonAsync();
        var billId = generateJson["data"]?["id"]?.GetValue<Guid>() ?? Guid.Empty;
        Assert.NotEqual(Guid.Empty, billId);
        Assert.True(generateJson["data"]?["totalAmount"]?.GetValue<decimal>() > 0);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/third-party-bills")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/v1/third-party-bills/{billId}")).StatusCode);
    }
}
