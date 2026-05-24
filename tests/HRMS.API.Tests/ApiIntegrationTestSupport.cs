using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using HRMS.API.Services;
using HRMS.Domain.Entities;
using HRMS.Domain.Enums;
using HRMS.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace HRMS.API.Tests;

public sealed class HrmsApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"hrms-tests-{Guid.NewGuid():N}";

    public TestSeedState SeedState { get; private set; } = new();
    public FakeProcessCenterClient FakeProcessCenterClient { get; } = new();
    public FakeTodoCenterGateway FakeTodoCenterGateway { get; } = new();
    public FakeWorkflowProcessCenterGateway FakeWorkflowProcessCenterGateway { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<HrmsDbContext>));
            services.RemoveAll(typeof(HrmsDbContext));
            services.RemoveAll(typeof(IProcessCenterClient));
            services.RemoveAll(typeof(ITodoCenterGateway));
            services.RemoveAll(typeof(IWorkflowProcessCenterGateway));

            services.AddDbContext<HrmsDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
            });
            services.AddSingleton<IProcessCenterClient>(FakeProcessCenterClient);
            services.AddSingleton<ITodoCenterGateway>(FakeTodoCenterGateway);
            services.AddSingleton<IWorkflowProcessCenterGateway>(FakeWorkflowProcessCenterGateway);
        });
    }

    public async Task ResetAsync()
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<HrmsDbContext>();

        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();

        SeedState = await TestDataSeeder.SeedAsync(dbContext);
    }

    public async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        await ResetAsync();

        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        var token = await LoginAsAdminAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public static async Task<string> LoginAsAdminAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            username = "admin",
            password = "123456"
        });

        response.EnsureSuccessStatusCode();

        var json = await response.ReadJsonAsync();
        return json["data"]?["token"]?.GetValue<string>()
            ?? throw new InvalidOperationException("登录响应缺少 token。");
    }

    public async Task<T> WithDbContextAsync<T>(Func<HrmsDbContext, Task<T>> action)
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<HrmsDbContext>();
        return await action(dbContext);
    }
}

public sealed class FakeProcessCenterClient : IProcessCenterClient
{
    public List<ProcessCenterStartCall> Calls { get; } = [];

    public Task<ProcessStartResultDto> StartPayrollApprovalAsync(
        PayrollApprovalStartRequest request,
        CancellationToken cancellationToken = default)
    {
        Calls.Add(new ProcessCenterStartCall(
            request.Run.Id,
            request.Run.RunNo,
            request.Run.YearMonth,
            request.StarterId,
            request.StarterName,
            request.SourcePayload));

        return Task.FromResult(new ProcessStartResultDto
        {
            ProcessCode = "PAYROLL_APPROVAL",
            RequestId = $"payroll-run:{request.Run.Id}:submit-approval",
            ProcessInstanceId = $"PROC-{Calls.Count:D4}",
            CurrentNodeId = "manager-approve",
            CurrentNodeName = "部门经理审批",
            CurrentAssigneeId = "u1001",
            CurrentAssigneeName = "部门经理"
        });
    }
}

public sealed record ProcessCenterStartCall(
    Guid RunId,
    string RunNo,
    string YearMonth,
    string StarterId,
    string StarterName,
    PayrollApprovalSourcePayload SourcePayload);

public sealed class FakeTodoCenterGateway : ITodoCenterGateway
{
    public List<string> Calls { get; } = [];

    public Task<JsonElement> GetMyTasksAsync(
        string? assigneeId,
        string? status,
        string? businessType,
        string? taskTypeCode,
        string? businessId,
        string? processInstanceId,
        string? keyword,
        CancellationToken cancellationToken = default)
    {
        Calls.Add($"list:{assigneeId}:{status}:{taskTypeCode}:{businessId}:{processInstanceId}:{keyword}");
        return Task.FromResult(ParseJson(
            """
            [
              {
                "id": 1,
                "taskNo": "TD202605240001",
                "title": "2026-05 薪资审批 - 财务审批",
                "businessType": "PayrollRun",
                "businessId": "RUN-2026-05",
                "processInstanceId": "1001",
                "processNodeId": "finance-approve",
                "assigneeId": "u1002",
                "assigneeName": "财务经理",
                "status": "Pending",
                "priority": 2,
                "sourcePayloadData": {
                  "scene": "PayrollApproval",
                  "yearMonth": "2026-05",
                  "totalNetSalary": 12345.67
                }
              }
            ]
            """));
    }

    public Task<JsonElement> GetTaskDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        Calls.Add($"detail:{id}");
        return Task.FromResult(ParseJson(
            """
            {
              "task": {
                "id": 1,
                "taskNo": "TD202605240001",
                "title": "2026-05 薪资审批 - 财务审批",
                "businessType": "PayrollRun",
                "businessId": "RUN-2026-05",
                "status": "Pending",
                "assigneeId": "u1002",
                "assigneeName": "财务经理",
                "sourcePayloadData": {
                  "scene": "PayrollApproval",
                  "yearMonth": "2026-05",
                  "totalNetSalary": 12345.67
                }
              },
              "logs": [
                {
                  "id": 1,
                  "action": "Create",
                  "operatorName": "管理员",
                  "createdTime": "2026-05-24T00:00:00Z"
                }
              ],
              "notifyLogs": []
            }
            """));
    }

    public Task<JsonElement> CompleteTaskAsync(long id, TodoTaskActionDto request, CancellationToken cancellationToken = default)
    {
        Calls.Add($"complete:{id}:{request.OperatorId}");
        return Task.FromResult(ParseJson(
            """
            {
              "id": 1,
              "status": "Completed",
              "result": "Completed"
            }
            """));
    }

    public Task<JsonElement> RejectTaskAsync(long id, TodoTaskActionDto request, CancellationToken cancellationToken = default)
    {
        Calls.Add($"reject:{id}:{request.OperatorId}");
        return Task.FromResult(ParseJson(
            """
            {
              "id": 1,
              "status": "Rejected",
              "result": "Rejected"
            }
            """));
    }

    public Task<JsonElement> TransferTaskAsync(long id, TodoTaskTransferDto request, CancellationToken cancellationToken = default)
    {
        Calls.Add($"transfer:{id}:{request.TargetAssigneeId}");
        return Task.FromResult(ParseJson(
            """
            {
              "originalTask": {
                "id": 1,
                "status": "Transferred"
              },
              "newTask": {
                "id": 2,
                "status": "Pending",
                "assigneeId": "u3001",
                "assigneeName": "代理审批人"
              }
            }
            """));
    }

    public Task<JsonElement> BatchCompleteAsync(TodoBatchCompleteDto request, CancellationToken cancellationToken = default)
    {
        Calls.Add($"batch:{request.TaskIds.Count}:{request.OperatorId}");
        return Task.FromResult(ParseJson(
            """
            {
              "completedCount": 2,
              "tasks": [
                { "id": 1, "status": "Completed" },
                { "id": 2, "status": "Completed" }
              ]
            }
            """));
    }

    public Task<JsonElement> BatchRejectAsync(TodoBatchRejectDto request, CancellationToken cancellationToken = default)
    {
        Calls.Add($"batch-reject:{request.TaskIds.Count}:{request.OperatorId}");
        return Task.FromResult(ParseJson(
            """
            {
              "rejectedCount": 2,
              "tasks": [
                { "id": 1, "status": "Rejected" },
                { "id": 2, "status": "Rejected" }
              ]
            }
            """));
    }

    public Task<JsonElement> UrgeTaskAsync(long id, TodoTaskActionDto request, CancellationToken cancellationToken = default)
    {
        Calls.Add($"urge:{id}:{request.OperatorId}");
        return Task.FromResult(ParseJson(
            """
            {
              "taskId": 1,
              "notifyType": "Urge",
              "receiverName": "财务经理"
            }
            """));
    }

    public Task<JsonElement> CreateAgentSettingAsync(CreateAgentSettingDto request, CancellationToken cancellationToken = default)
    {
        Calls.Add($"agent-create:{request.PrincipalUserId}:{request.AgentUserId}");
        return Task.FromResult(ParseJson(
            """
            {
              "id": 1,
              "principalUserId": "admin-id",
              "agentUserId": "agent-id",
              "scopeType": "All",
              "status": "Active"
            }
            """));
    }

    public Task<JsonElement> GetAgentSettingsAsync(string? principalUserId, CancellationToken cancellationToken = default)
    {
        Calls.Add($"agent-list:{principalUserId}");
        return Task.FromResult(ParseJson(
            """
            [
              {
                "id": 1,
                "principalUserId": "admin-id",
                "agentUserId": "agent-id",
                "scopeType": "All",
                "status": "Active",
                "startTime": "2026-05-24T00:00:00Z",
                "endTime": "2026-05-25T00:00:00Z"
              }
            ]
            """));
    }

    public Task<JsonElement> GetKpiSummaryAsync(string? assigneeId, CancellationToken cancellationToken = default)
    {
        Calls.Add($"kpi:{assigneeId}");
        return Task.FromResult(ParseJson(
            """
            {
              "totalCount": 3,
              "pendingCount": 2,
              "completedCount": 1,
              "timeoutCount": 0,
              "onTimeRate": 1.0
            }
            """));
    }

    private static JsonElement ParseJson(string json)
        => JsonDocument.Parse(json).RootElement.Clone();
}

public sealed class FakeWorkflowProcessCenterGateway : IWorkflowProcessCenterGateway
{
    public List<string> Calls { get; } = [];

    public Task<JsonElement> GetDefinitionsAsync(CancellationToken cancellationToken = default)
    {
        Calls.Add("definitions");
        return Task.FromResult(ParseJson(
            """
            [
              {
                "id": 1,
                "processCode": "PAYROLL_APPROVAL",
                "processName": "薪资审批流程",
                "businessType": "PayrollRun",
                "versionNo": 1,
                "status": "Published",
                "nodes": [
                  {
                    "nodeId": "manager-approve",
                    "nodeName": "部门经理审批",
                    "assigneeId": "u1001",
                    "assigneeName": "部门经理",
                    "sortOrder": 1
                  }
                ]
              }
            ]
            """));
    }

    public Task<JsonElement> GetDefinitionAsync(long id, CancellationToken cancellationToken = default)
    {
        Calls.Add($"definition:{id}");
        return Task.FromResult(ParseJson(
            """
            {
              "id": 1,
              "processCode": "PAYROLL_APPROVAL",
              "processName": "薪资审批流程",
              "businessType": "PayrollRun",
              "versionNo": 1,
              "status": "Published",
              "diagramXml": "<bpmn />"
            }
            """));
    }

    public Task<JsonElement> SaveDefinitionAsync(long? id, SaveProcessDefinitionDto request, CancellationToken cancellationToken = default)
    {
        Calls.Add($"save-definition:{id}:{request.ProcessCode}");
        return Task.FromResult(ParseJson(
            $$"""
            {
              "id": {{id ?? 1}},
              "processCode": "{{request.ProcessCode}}",
              "processName": "{{request.ProcessName}}",
              "businessType": "{{request.BusinessType}}",
              "versionNo": {{request.VersionNo}},
              "status": "{{(request.IsPublished ? "Published" : "Draft")}}"
            }
            """));
    }

    public Task<JsonElement> PublishDefinitionAsync(long id, CancellationToken cancellationToken = default)
    {
        Calls.Add($"publish-definition:{id}");
        return Task.FromResult(ParseJson(
            $$"""
            {
              "id": {{id}},
              "processCode": "PAYROLL_APPROVAL",
              "processName": "薪资审批流程",
              "businessType": "PayrollRun",
              "versionNo": 1,
              "status": "Published"
            }
            """));
    }

    public Task<JsonElement> StartProcessAsync(StartProcessDto request, CancellationToken cancellationToken = default)
    {
        Calls.Add($"start:{request.ProcessCode}:{request.BusinessId}:{request.StarterId}");
        return Task.FromResult(ParseJson(
            $$"""
            {
              "id": 2001,
              "instanceNo": "PI202605240999",
              "processCode": "{{request.ProcessCode}}",
              "businessType": "{{request.BusinessType}}",
              "businessId": "{{request.BusinessId}}",
              "title": "{{request.Title}}",
              "status": "Running",
              "starterId": "{{request.StarterId}}",
              "starterName": "{{request.StarterName}}"
            }
            """));
    }

    public Task<JsonElement> GetInstancesAsync(
        string? processCode,
        string? status,
        string? businessType,
        string? businessId,
        string? keyword,
        CancellationToken cancellationToken = default)
    {
        Calls.Add($"instances:{processCode}:{status}:{businessType}:{businessId}:{keyword}");
        return Task.FromResult(ParseJson(
            """
            [
              {
                "id": 1001,
                "instanceNo": "PI202605240001",
                "processCode": "PAYROLL_APPROVAL",
                "businessType": "PayrollRun",
                "businessId": "RUN-2026-05",
                "title": "2026-05 薪资审批",
                "status": "Running",
                "currentNodeName": "财务审批",
                "starterName": "管理员",
                "currentTodo": {
                  "assigneeId": "u1002",
                  "assigneeName": "财务经理"
                }
              }
            ]
            """));
    }

    public Task<JsonElement> GetInstanceAsync(long id, CancellationToken cancellationToken = default)
    {
        Calls.Add($"instance:{id}");
        return Task.FromResult(ParseJson(
            """
            {
              "id": 1001,
              "instanceNo": "PI202605240001",
              "processCode": "PAYROLL_APPROVAL",
              "businessType": "PayrollRun",
              "businessId": "RUN-2026-05",
              "title": "2026-05 薪资审批",
              "status": "Running",
              "currentNodeName": "财务审批",
              "starterName": "管理员",
              "history": [
                {
                  "id": 1,
                  "nodeName": "部门经理审批",
                  "action": "Complete",
                  "actionResult": "Completed"
                },
                {
                  "id": 2,
                  "nodeName": "财务审批",
                  "action": "Arrive",
                  "actionResult": "Pending"
                }
              ]
            }
            """));
    }

    public Task<JsonElement> RejectInstanceAsync(long id, ProcessInstanceActionDto request, CancellationToken cancellationToken = default)
    {
        Calls.Add($"reject:{id}:{request.OperatorId}");
        return Task.FromResult(ParseJson(
            """
            {
              "id": 1001,
              "status": "Rejected",
              "currentNodeName": "财务审批"
            }
            """));
    }

    public Task<JsonElement> RebuildCurrentTodoAsync(long id, ProcessInstanceActionDto request, CancellationToken cancellationToken = default)
    {
        Calls.Add($"rebuild:{id}:{request.OperatorId}");
        return Task.FromResult(ParseJson(
            """
            {
              "instance": {
                "id": 1001,
                "status": "Running",
                "currentNodeName": "财务审批"
              },
              "currentTodo": {
                "processNodeId": "finance-approve",
                "assigneeId": "u1002",
                "assigneeName": "财务经理"
              },
              "task": {
                "id": 501,
                "taskNo": "TD-REBUILD-0001",
                "status": "Pending",
                "assigneeId": "u1002",
                "assigneeName": "财务经理"
              }
            }
            """));
    }

    public Task<JsonElement> TerminateInstanceAsync(long id, ProcessInstanceActionDto request, CancellationToken cancellationToken = default)
    {
        Calls.Add($"terminate:{id}:{request.OperatorId}");
        return Task.FromResult(ParseJson(
            """
            {
              "id": 1001,
              "status": "Terminated",
              "currentNodeName": "财务审批"
            }
            """));
    }

    public Task<JsonElement> GetCallbackLogsAsync(long id, CancellationToken cancellationToken = default)
    {
        Calls.Add($"callback-logs:{id}");
        return Task.FromResult(ParseJson(
            """
            [
              {
                "id": 9001,
                "callbackType": "Business",
                "status": "Success",
                "createdTime": "2026-05-24T00:00:00Z"
              }
            ]
            """));
    }

    public Task<JsonElement> RetryCallbackAsync(long id, long logId, CancellationToken cancellationToken = default)
    {
        Calls.Add($"retry-callback:{id}:{logId}");
        return Task.FromResult(ParseJson(
            """
            {
              "logId": 9001,
              "status": "Success"
            }
            """));
    }

    private static JsonElement ParseJson(string json)
        => JsonDocument.Parse(json).RootElement.Clone();
}

public sealed class TestSeedState
{
    public Guid HeadquartersId { get; init; }
    public Guid BranchId { get; init; }
    public Guid DepartmentId { get; init; }
    public Guid CustomerId { get; init; }
    public Guid SupplierId { get; init; }
    public Guid InternalEmployeeId { get; init; }
    public Guid ThirdPartyEmployeeId { get; init; }
    public Guid AdminUserId { get; init; }
    public Guid AdminRoleId { get; init; }
    public Guid AdminPostId { get; init; }
    public Dictionary<string, Guid> MenuIds { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

internal static class TestDataSeeder
{
    public static async Task<TestSeedState> SeedAsync(HrmsDbContext dbContext)
    {
        var headquarter = new OrgUnit
        {
            Code = "HQ",
            Name = "总公司（测试）",
            Level = OrgLevel.Headquarters
        };
        var branch = new OrgUnit
        {
            Code = "BRANCH-NJ",
            Name = "南京分公司",
            Level = OrgLevel.Branch,
            ParentId = headquarter.Id
        };
        var department = new OrgUnit
        {
            Code = "NJ-HR",
            Name = "人事部",
            Level = OrgLevel.Department,
            ParentId = branch.Id
        };
        var supplier = new OrgUnit
        {
            Code = "SUPPLIER-01",
            Name = "供应商一号",
            Level = OrgLevel.Supplier
        };
        var customer = new Customer
        {
            Code = "CUS-TEST-01",
            Name = "测试客户一号",
            ShortName = "测试客户",
            ContactPerson = "客户联系人",
            Phone = "13800000999",
            TaxNo = "91320000CUSTEST01",
            InvoiceTitle = "测试客户一号有限公司"
        };
        var supplierProfile = new Supplier
        {
            Id = supplier.Id,
            Code = supplier.Code,
            Name = supplier.Name,
            ContactPerson = "供应商联系人",
            Phone = "13800000888",
            TaxNo = "91320000SUPTEST01",
            BankName = "中国银行",
            BankAccount = "6222000000000000000",
            PaymentTermDays = 30
        };

        var internalEmployee = new Employee
        {
            EmployeeNo = "10001",
            Name = "张三",
            IdCard = "110105199001011234",
            Phone = "13800000001",
            EmployeeType = EmployeeType.Internal,
            SalaryMode = SalaryMode.Hourly,
            OrgUnitId = department.Id,
            HourlyRate = 25m,
            Tags = new List<string> { "正式员工" }
        };
        var thirdPartyEmployee = new Employee
        {
            EmployeeNo = "20001",
            Name = "李四",
            IdCard = "110105199002021235",
            Phone = "13800000002",
            EmployeeType = EmployeeType.ThirdParty,
            SalaryMode = SalaryMode.Hourly,
            OrgUnitId = department.Id,
            ThirdPartyCompanyId = supplier.Id,
            HourlyRate = 30m,
            Tags = new List<string> { "外包" }
        };

        dbContext.OrgUnits.AddRange(headquarter, branch, department, supplier);
        dbContext.Customers.Add(customer);
        dbContext.Suppliers.Add(supplierProfile);
        dbContext.Employees.AddRange(internalEmployee, thirdPartyEmployee);

        var menus = BuildMenus();
        dbContext.SysMenus.AddRange(menus.Values);

        var adminRole = new SysRole
        {
            RoleCode = "ADMIN",
            RoleName = "系统管理员",
            Description = "测试管理员"
        };
        var adminPost = new SysPost
        {
            PostCode = "ADMIN_POST",
            PostName = "系统管理岗",
            Description = "测试岗位"
        };
        var adminUser = new SysUser
        {
            Username = "admin",
            Password = "123456",
            Name = "管理员",
            IsAdmin = true,
            PostId = adminPost.Id
        };

        dbContext.SysRoles.Add(adminRole);
        dbContext.SysPosts.Add(adminPost);
        dbContext.SysUsers.Add(adminUser);
        dbContext.SysUserRoles.Add(new SysUserRole
        {
            UserId = adminUser.Id,
            RoleId = adminRole.Id
        });

        dbContext.SysRolePermissions.AddRange(menus.Values.Select(menu => new SysRolePermission
        {
            RoleId = adminRole.Id,
            MenuId = menu.Id,
            PermissionCode = menu.PermissionCode ?? menu.MenuKey,
            PermissionType = menu.MenuType
        }));

        dbContext.SysPostPermissions.AddRange(menus.Values.Select(menu => new SysPostPermission
        {
            PostId = adminPost.Id,
            MenuId = menu.Id,
            PermissionCode = menu.PermissionCode ?? menu.MenuKey,
            PermissionType = menu.MenuType
        }));

        dbContext.SysConfigParams.Add(new SysConfigParam
        {
            Category = "security",
            ParamKey = "loginCaptchaEnabled",
            ParamValue = "false",
            Description = "是否启用登录验证码"
        });

        await dbContext.SaveChangesAsync();

        return new TestSeedState
        {
            HeadquartersId = headquarter.Id,
            BranchId = branch.Id,
            DepartmentId = department.Id,
            CustomerId = customer.Id,
            SupplierId = supplier.Id,
            InternalEmployeeId = internalEmployee.Id,
            ThirdPartyEmployeeId = thirdPartyEmployee.Id,
            AdminUserId = adminUser.Id,
            AdminRoleId = adminRole.Id,
            AdminPostId = adminPost.Id,
            MenuIds = menus.ToDictionary(x => x.Key, x => x.Value.Id, StringComparer.OrdinalIgnoreCase)
        };
    }

    private static Dictionary<string, SysMenu> BuildMenus()
    {
        var definitions = GetMenuDefinitions();
        var menus = definitions.ToDictionary(
            definition => definition.MenuKey,
            definition => new SysMenu
            {
                MenuKey = definition.MenuKey,
                MenuName = definition.MenuName,
                MenuType = definition.MenuType,
                SortOrder = definition.SortOrder,
                RoutePath = definition.RoutePath,
                ComponentPath = definition.ComponentPath,
                Icon = definition.Icon,
                IsVisible = definition.IsVisible,
                IsActive = true,
                PermissionCode = definition.PermissionCode
            },
            StringComparer.OrdinalIgnoreCase);

        foreach (var definition in definitions.Where(x => !string.IsNullOrWhiteSpace(x.ParentKey)))
        {
            menus[definition.MenuKey].ParentId = menus[definition.ParentKey!].Id;
        }

        return menus;
    }

    private static List<MenuSeedSnapshot> GetMenuDefinitions()
    {
        var method = typeof(HRMS.API.Services.RbacDataInitializer).GetMethod(
            "GetMenuDefinitions",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var result = method!.Invoke(null, null);
        Assert.NotNull(result);

        return ((System.Collections.IEnumerable)result)
            .Cast<object>()
            .Select(item => new MenuSeedSnapshot(
                GetRequiredString(item, "MenuKey"),
                GetOptionalString(item, "ParentKey"),
                GetRequiredString(item, "MenuName"),
                GetRequiredString(item, "MenuType"),
                GetRequiredInt(item, "SortOrder"),
                GetOptionalString(item, "RoutePath"),
                GetOptionalString(item, "ComponentPath"),
                GetOptionalString(item, "Icon"),
                GetRequiredBool(item, "IsVisible"),
                GetRequiredString(item, "PermissionCode")))
            .ToList();
    }

    private static string GetRequiredString(object instance, string propertyName)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(property);

        var value = property!.GetValue(instance) as string;
        Assert.False(string.IsNullOrWhiteSpace(value), $"菜单定义缺少 {propertyName}");
        return value!;
    }

    private static string? GetOptionalString(object instance, string propertyName)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(property);
        return property!.GetValue(instance) as string;
    }

    private static int GetRequiredInt(object instance, string propertyName)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(property);
        return (int)(property!.GetValue(instance) ?? 0);
    }

    private static bool GetRequiredBool(object instance, string propertyName)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(property);
        return (bool)(property!.GetValue(instance) ?? false);
    }

    private sealed record MenuSeedSnapshot(
        string MenuKey,
        string? ParentKey,
        string MenuName,
        string MenuType,
        int SortOrder,
        string? RoutePath,
        string? ComponentPath,
        string? Icon,
        bool IsVisible,
        string PermissionCode);
}

internal static class HttpResponseMessageExtensions
{
    public static async Task<JsonNode> ReadJsonAsync(this HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonNode.Parse(content, nodeOptions: new JsonNodeOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidOperationException("响应不是有效 JSON。");
    }

    public static async Task<int> ReadCodeAsync(this HttpResponseMessage response)
    {
        var json = await response.ReadJsonAsync();
        return json["code"]?.GetValue<int>() ?? -1;
    }
}

internal static class TestApiHelper
{
    public static StringContent JsonContent(object value)
    {
        return new StringContent(JsonSerializer.Serialize(value), System.Text.Encoding.UTF8, "application/json");
    }

    public static async Task<HttpResponseMessage> PostSignedJsonAsync(HttpClient client, string path, object payload)
    {
        var payloadJson = JsonSerializer.Serialize(payload);
        using var request = new HttpRequestMessage(HttpMethod.Post, path)
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

        return await client.SendAsync(request);
    }

    public static async Task<Guid> CreateTimesheetAndApproveAsync(
        HttpClient client,
        Guid employeeId,
        Guid actualOrgUnitId,
        Guid approverId,
        DateTime date,
        decimal workingHours)
    {
        var createResponse = await client.PostAsJsonAsync("/api/v1/timesheets", new
        {
            employeeId,
            date,
            actualOrgUnitId,
            workingHours,
            shiftType = 0,
            remark = "integration-test"
        });

        Assert.Equal(201, (int)createResponse.StatusCode);
        var createJson = await createResponse.ReadJsonAsync();
        var timesheetId = createJson["data"]?["id"]?.GetValue<Guid>()
            ?? throw new InvalidOperationException("工时创建响应缺少 id。");

        var approveResponse = await client.PostAsync(
            $"/api/v1/timesheets/{timesheetId}/approve?approverId={approverId}",
            content: null);

        Assert.Equal(200, (int)approveResponse.StatusCode);
        return timesheetId;
    }
}
