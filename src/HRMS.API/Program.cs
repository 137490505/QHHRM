using HRMS.Application.DTOs;
using HRMS.Application.Services;
using HRMS.API.Services;
using HRMS.Domain.Interfaces;
using HRMS.Infrastructure.Data;
using HRMS.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Diagnostics;
using Newtonsoft.Json;

var builder = WebApplication.CreateBuilder(args);
var appStartedAt = DateTimeOffset.UtcNow;

if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_URLS")))
{
    builder.WebHost.UseUrls("http://localhost:5000");
}

builder.Services.AddDbContext<HrmsDbContext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))));

builder.Services.AddScoped<IOrgUnitRepository, OrgUnitRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<ISupplierRepository, SupplierRepository>();
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<ITimesheetRepository, TimesheetRepository>();
builder.Services.AddScoped<ISalaryCalculationRepository, SalaryCalculationRepository>();
builder.Services.AddScoped<IEmployeeTypeRepository, EmployeeTypeRepository>();
builder.Services.AddScoped<IEmployeePayrollProfileRepository, EmployeePayrollProfileRepository>();
builder.Services.AddScoped<ISalaryRuleRepository, SalaryRuleRepository>();
builder.Services.AddScoped<IOvertimeRateConfigRepository, OvertimeRateConfigRepository>();
builder.Services.AddScoped<IHolidayRuleRepository, HolidayRuleRepository>();
builder.Services.AddScoped<IAttendanceRepository, AttendanceRepository>();
builder.Services.AddScoped<ISalaryAdjustmentRepository, SalaryAdjustmentRepository>();
builder.Services.AddScoped<IIncomeTaxRuleRepository, IncomeTaxRuleRepository>();
builder.Services.AddScoped<IPayrollRunRepository, PayrollRunRepository>();
builder.Services.AddScoped<IPayrollRepository, PayrollRepository>();
builder.Services.AddScoped<IThirdPartyBillRepository, ThirdPartyBillRepository>();

builder.Services.AddScoped<OrgUnitService>();
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<SupplierService>();
builder.Services.AddScoped<EmployeeService>();
builder.Services.AddScoped<TimesheetService>();
builder.Services.AddScoped<SalaryCalculationService>();
builder.Services.AddScoped<EmployeeTypeService>();
builder.Services.AddScoped<SalaryRuleService>();
builder.Services.AddScoped<OvertimeRateConfigService>();
builder.Services.AddScoped<HolidayRuleService>();
builder.Services.AddScoped<IncomeTaxRuleService>();
builder.Services.AddScoped<PayrollCalculationService>();
builder.Services.AddScoped<EmployeePayrollProfileService>();
builder.Services.AddScoped<AttendanceService>();
builder.Services.AddScoped<SalaryAdjustmentService>();
builder.Services.AddScoped<ThirdPartyBillService>();
builder.Services.AddScoped<AccessControlService>();
builder.Services.AddScoped<SettingsService>();
builder.Services.AddScoped<RbacDataInitializer>();
builder.Services.AddScoped<CurrentUserAccessor>();
builder.Services.AddScoped<PayrollApprovalWorkflowService>();
builder.Services.AddScoped<ProcessedCallbackEventStore>();
builder.Services.AddScoped<CallbackSecurityService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<TokenSessionStore>();
builder.Services.Configure<ProcessCenterOptions>(builder.Configuration.GetSection("ProcessCenter"));
builder.Services.Configure<TodoCenterOptions>(builder.Configuration.GetSection("TodoCenter"));
builder.Services.Configure<InternalCallbackOptions>(builder.Configuration.GetSection("InternalCallbacks"));
builder.Services.AddHttpClient<IProcessCenterClient, ProcessCenterHttpClient>();
builder.Services.AddHttpClient<ITodoCenterGateway, TodoCenterGateway>();
builder.Services.AddHttpClient<IWorkflowProcessCenterGateway, WorkflowProcessCenterGateway>();


builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "HRMS API", Version = "v1", Description = "人力资源管理系统 API" });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVue", policy =>
    {
        policy.SetIsOriginAllowed(origin =>
              {
                  if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
                  {
                      return false;
                  }

                  return (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                      && (string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase)
                          || string.Equals(uri.Host, "127.0.0.1", StringComparison.OrdinalIgnoreCase));
              })
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(10);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<HrmsDbContext>();
    var initializer = scope.ServiceProvider.GetRequiredService<RbacDataInitializer>();

    await dbContext.Database.EnsureDeletedAsync();
    await dbContext.Database.EnsureCreatedAsync();
    await initializer.InitializeAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "HRMS API v1"));
}

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        if (exception is InvalidOperationException)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json; charset=utf-8";
            await context.Response.WriteAsJsonAsync(new
            {
                code = 400,
                message = exception.Message
            });
            return;
        }

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json; charset=utf-8";
        await context.Response.WriteAsJsonAsync(new
        {
            code = 500,
            message = "服务器内部错误"
        });
    });
});

app.UseHttpsRedirection();
app.UseSession();
app.UseCors("AllowVue");
app.UseAuthorization();
app.MapGet("/health", (IHostEnvironment environment) => Results.Ok(new
{
    status = "Healthy",
    service = "HRMS.API",
    environment = environment.EnvironmentName,
    startedAt = appStartedAt,
    now = DateTimeOffset.UtcNow
}));
app.MapControllers();

app.Run();

public partial class Program { }
