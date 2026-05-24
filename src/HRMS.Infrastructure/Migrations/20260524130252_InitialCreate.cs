using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "employee_type",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    TypeCode = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TypeName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SalaryMode = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    HasOvertime = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    HasMealSubsidy = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    HasNightSubsidy = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    HasPerformance = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    HasSocialSecurity = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Remark = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_type", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "holiday_rule",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    HolidayCode = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    HolidayName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    HolidayDate = table.Column<DateTime>(type: "date", nullable: false),
                    HolidayType = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OvertimeMultiplier = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Remark = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_holiday_rule", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "income_tax_rule",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    RuleYear = table.Column<int>(type: "int", nullable: false),
                    LevelNo = table.Column<int>(type: "int", nullable: false),
                    MinTaxableAmount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    MaxTaxableAmount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    TaxRate = table.Column<decimal>(type: "decimal(6,4)", precision: 6, scale: 4, nullable: false),
                    QuickDeduction = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    ThresholdAmount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Remark = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_income_tax_rule", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "org_unit",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Code = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Level = table.Column<int>(type: "int", nullable: false),
                    ParentId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    ManagerId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_org_unit", x => x.Id);
                    table.ForeignKey(
                        name: "FK_org_unit_org_unit_ParentId",
                        column: x => x.ParentId,
                        principalTable: "org_unit",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "payroll_run",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    RunNo = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    YearMonth = table.Column<string>(type: "varchar(7)", maxLength: 7, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RunType = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EmployeeScopeJson = table.Column<string>(type: "json", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RuleSnapshotJson = table.Column<string>(type: "json", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TriggeredBy = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ApprovalProcessCode = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ApprovalProcessInstanceId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ApprovalRequestId = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ApprovalSubmittedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ApprovedBy = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ApprovedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    FinishedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Remark = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payroll_run", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sys_config_param",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Category = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ParamKey = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ParamValue = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OrgUnitId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    IsGlobal = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    TakeEffectImmediately = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ScheduledTakeEffectDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ChangeReason = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_config_param", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sys_menu",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    MenuKey = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MenuName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ParentId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    MenuType = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    RoutePath = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ComponentPath = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Icon = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsVisible = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    PermissionCode = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_menu", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sys_menu_sys_menu_ParentId",
                        column: x => x.ParentId,
                        principalTable: "sys_menu",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sys_post",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    PostCode = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PostName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_post", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sys_processed_event",
                columns: table => new
                {
                    EventId = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProcessedTime = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_processed_event", x => x.EventId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sys_role",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    RoleCode = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RoleName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OrgUnitId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_role", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sys_token_session",
                columns: table => new
                {
                    Token = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    IsAdmin = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    PermissionsJson = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_token_session", x => x.Token);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "overtime_rate_config",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ConfigCode = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EmployeeTypeId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    HolidayType = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Multiplier = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    EffectiveStart = table.Column<DateTime>(type: "date", nullable: false),
                    EffectiveEnd = table.Column<DateTime>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Remark = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_overtime_rate_config", x => x.Id);
                    table.ForeignKey(
                        name: "FK_overtime_rate_config_employee_type_EmployeeTypeId",
                        column: x => x.EmployeeTypeId,
                        principalTable: "employee_type",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "salary_rule",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    RuleCode = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RuleName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EmployeeTypeId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    EffectiveStart = table.Column<DateTime>(type: "date", nullable: false),
                    EffectiveEnd = table.Column<DateTime>(type: "date", nullable: true),
                    FixedSalary = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    HourlyRate = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    PieceworkUnitPrice = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    BaseSalaryForPiecework = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    MealSubsidyPerDay = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    NightSubsidyPerDay = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    PerformanceBase = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    DefaultOvertimeMultiplier = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Remark = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_salary_rule", x => x.Id);
                    table.ForeignKey(
                        name: "FK_salary_rule_employee_type_EmployeeTypeId",
                        column: x => x.EmployeeTypeId,
                        principalTable: "employee_type",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "employee",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    EmployeeNo = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Gender = table.Column<int>(type: "int", nullable: false),
                    IdCard = table.Column<string>(type: "varchar(18)", maxLength: 18, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Phone = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Email = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EmployeeType = table.Column<int>(type: "int", nullable: false),
                    SalaryMode = table.Column<int>(type: "int", nullable: false),
                    OrgUnitId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ThirdPartyCompanyId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    JobTitle = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Level = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Tags = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    HourlyRate = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    MonthlySalary = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    PieceRatePrice = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    SocialSecurityBase = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    HousingFundBase = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    TrialEndDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    TrialDaysRemaining = table.Column<int>(type: "int", nullable: false),
                    ProbationDays = table.Column<int>(type: "int", nullable: false),
                    ContractType = table.Column<int>(type: "int", nullable: false),
                    ContractStartDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ContractEndDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    HireDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsBlacklisted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DismissDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DismissReason = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee", x => x.Id);
                    table.ForeignKey(
                        name: "FK_employee_org_unit_OrgUnitId",
                        column: x => x.OrgUnitId,
                        principalTable: "org_unit",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_employee_org_unit_ThirdPartyCompanyId",
                        column: x => x.ThirdPartyCompanyId,
                        principalTable: "org_unit",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "expense_application",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ApplicationNo = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ExpenseType = table.Column<int>(type: "int", nullable: false),
                    ApplicantId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    OrgUnitId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Amount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ApproverId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    ApprovedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ApprovalComment = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AttachmentUrls = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BudgetId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_expense_application", x => x.Id);
                    table.ForeignKey(
                        name: "FK_expense_application_org_unit_OrgUnitId",
                        column: x => x.OrgUnitId,
                        principalTable: "org_unit",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "product",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Code = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Unit = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ClientUnitPrice = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    ClientPriceEffectiveFrom = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ClientPriceEffectiveTo = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    EmployeeUnitPrice = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    EmployeePriceEffectiveFrom = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    EmployeePriceEffectiveTo = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    OrgUnitId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product", x => x.Id);
                    table.ForeignKey(
                        name: "FK_product_org_unit_OrgUnitId",
                        column: x => x.OrgUnitId,
                        principalTable: "org_unit",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "third_party_bill",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    BillNo = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ClientId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    TotalWages = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    ManagementFee = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    ManagementFeeRate = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    ManagementFeeType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OtherFees = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ConfirmedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    InvoiceNo = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    InvoicedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    PaidAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Remark = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_third_party_bill", x => x.Id);
                    table.ForeignKey(
                        name: "FK_third_party_bill_org_unit_ClientId",
                        column: x => x.ClientId,
                        principalTable: "org_unit",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sys_post_permission",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    PostId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    MenuId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    PermissionCode = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PermissionType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_post_permission", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sys_post_permission_sys_menu_MenuId",
                        column: x => x.MenuId,
                        principalTable: "sys_menu",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_sys_post_permission_sys_post_PostId",
                        column: x => x.PostId,
                        principalTable: "sys_post",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sys_role_permission",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    RoleId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    MenuId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    PermissionCode = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PermissionType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_role_permission", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sys_role_permission_sys_menu_MenuId",
                        column: x => x.MenuId,
                        principalTable: "sys_menu",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_sys_role_permission_sys_role_RoleId",
                        column: x => x.RoleId,
                        principalTable: "sys_role",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "attendance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    EmployeeId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    WorkDate = table.Column<DateTime>(type: "date", nullable: false),
                    NormalHours = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    OvertimeHours = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    OvertimeType = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PieceworkQty = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    ShiftType = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsNightShift = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AttendanceSource = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SourceRecordId = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Remark = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attendance", x => x.Id);
                    table.ForeignKey(
                        name: "FK_attendance_employee_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "employee_payroll_profile",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    EmployeeId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    EmployeeTypeId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ShiftType = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BankAccount = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PayrollStatus = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    JoinPayrollDate = table.Column<DateTime>(type: "date", nullable: true),
                    LeavePayrollDate = table.Column<DateTime>(type: "date", nullable: true),
                    Remark = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_payroll_profile", x => x.Id);
                    table.ForeignKey(
                        name: "FK_employee_payroll_profile_employee_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_employee_payroll_profile_employee_type_EmployeeTypeId",
                        column: x => x.EmployeeTypeId,
                        principalTable: "employee_type",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "payroll",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    PayrollRunId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    EmployeeId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    EmployeeTypeId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    YearMonth = table.Column<string>(type: "varchar(7)", maxLength: 7, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EmployeeNoSnapshot = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EmployeeNameSnapshot = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OrgUnitIdSnapshot = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    SalaryModeSnapshot = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NormalWage = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    OvertimeWage = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    PieceworkWage = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    FixedSalary = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    BaseSalary = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    MealSubsidy = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    NightSubsidy = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    PerformanceBonus = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    OtherAllowance = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    SocialSecurityEmployee = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    SocialSecurityCompany = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    ProvidentFundEmployee = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    ProvidentFundCompany = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    IncomeTax = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    OtherDeduction = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    TotalGross = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    NetSalary = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    TotalCompanyCost = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CalculatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    PaidAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payroll", x => x.Id);
                    table.ForeignKey(
                        name: "FK_payroll_employee_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_payroll_employee_type_EmployeeTypeId",
                        column: x => x.EmployeeTypeId,
                        principalTable: "employee_type",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_payroll_payroll_run_PayrollRunId",
                        column: x => x.PayrollRunId,
                        principalTable: "payroll_run",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "salary_adjustment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    EmployeeId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    YearMonth = table.Column<string>(type: "varchar(7)", maxLength: 7, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AdjustmentType = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Amount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    SourceType = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SourceId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsAutoGenerated = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Remark = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedBy = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedBy = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_salary_adjustment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_salary_adjustment_employee_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "salary_calculation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    EmployeeId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    RegularHours = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    OvertimeHours = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    RegularWages = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    OvertimeWages = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    MealAllowance = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    Benefits = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    PerformanceBonus = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    AbsenceDeduction = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    OtherDeductions = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    GrossWages = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    SocialSecurityPersonal = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    HousingFundPersonal = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    IncomeTax = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    TotalDeductions = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    NetWages = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    CalculatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_salary_calculation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_salary_calculation_employee_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sys_user",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Username = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Password = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Phone = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Email = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EmployeeId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    PostId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsAdmin = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_user", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sys_user_employee_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employee",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_sys_user_sys_post_PostId",
                        column: x => x.PostId,
                        principalTable: "sys_post",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "timesheet",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    EmployeeId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Date = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ActualOrgUnitId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    WorkingHours = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    OvertimeHours = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    ShiftType = table.Column<int>(type: "int", nullable: false),
                    Remark = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ApprovalStatus = table.Column<int>(type: "int", nullable: false),
                    ApproverId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    ApprovedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_timesheet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_timesheet_employee_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_timesheet_org_unit_ActualOrgUnitId",
                        column: x => x.ActualOrgUnitId,
                        principalTable: "org_unit",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "output_record",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ProductId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Date = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    OrgUnitId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    QualifiedQuantity = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    IsConfirmedByClient = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ConfirmedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Remark = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_output_record", x => x.Id);
                    table.ForeignKey(
                        name: "FK_output_record_org_unit_OrgUnitId",
                        column: x => x.OrgUnitId,
                        principalTable: "org_unit",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_output_record_product_ProductId",
                        column: x => x.ProductId,
                        principalTable: "product",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "third_party_bill_detail",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ThirdPartyBillId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Tag = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TotalWages = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    TotalManagementFee = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    EmployeeCount = table.Column<int>(type: "int", nullable: false),
                    TotalHours = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_third_party_bill_detail", x => x.Id);
                    table.ForeignKey(
                        name: "FK_third_party_bill_detail_third_party_bill_ThirdPartyBillId",
                        column: x => x.ThirdPartyBillId,
                        principalTable: "third_party_bill",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "payroll_detail",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    PayrollId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ComponentCode = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ComponentName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ComponentCategory = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Amount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    UnitPrice = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    SourceType = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SourceId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Remark = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payroll_detail", x => x.Id);
                    table.ForeignKey(
                        name: "FK_payroll_detail_payroll_PayrollId",
                        column: x => x.PayrollId,
                        principalTable: "payroll",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "payslip",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    SalaryCalculationId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    EmployeeId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    PdfUrl = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsSigned = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    SignedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    HasComplaint = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ComplaintReason = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payslip", x => x.Id);
                    table.ForeignKey(
                        name: "FK_payslip_employee_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_payslip_salary_calculation_SalaryCalculationId",
                        column: x => x.SalaryCalculationId,
                        principalTable: "salary_calculation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sys_user_role",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    RoleId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_user_role", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sys_user_role_sys_role_RoleId",
                        column: x => x.RoleId,
                        principalTable: "sys_role",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_sys_user_role_sys_user_UserId",
                        column: x => x.UserId,
                        principalTable: "sys_user",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_attendance_AttendanceSource_SourceRecordId",
                table: "attendance",
                columns: new[] { "AttendanceSource", "SourceRecordId" });

            migrationBuilder.CreateIndex(
                name: "IX_attendance_EmployeeId_WorkDate",
                table: "attendance",
                columns: new[] { "EmployeeId", "WorkDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_attendance_WorkDate_Status",
                table: "attendance",
                columns: new[] { "WorkDate", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_employee_EmployeeNo",
                table: "employee",
                column: "EmployeeNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_employee_OrgUnitId",
                table: "employee",
                column: "OrgUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_employee_ThirdPartyCompanyId",
                table: "employee",
                column: "ThirdPartyCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_employee_payroll_profile_EmployeeId",
                table: "employee_payroll_profile",
                column: "EmployeeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_employee_payroll_profile_EmployeeTypeId_PayrollStatus",
                table: "employee_payroll_profile",
                columns: new[] { "EmployeeTypeId", "PayrollStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_employee_type_IsActive_SortOrder",
                table: "employee_type",
                columns: new[] { "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_employee_type_TypeCode",
                table: "employee_type",
                column: "TypeCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_employee_type_TypeName",
                table: "employee_type",
                column: "TypeName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_expense_application_OrgUnitId",
                table: "expense_application",
                column: "OrgUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_holiday_rule_HolidayCode",
                table: "holiday_rule",
                column: "HolidayCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_holiday_rule_HolidayDate_HolidayType",
                table: "holiday_rule",
                columns: new[] { "HolidayDate", "HolidayType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_holiday_rule_HolidayDate_IsActive",
                table: "holiday_rule",
                columns: new[] { "HolidayDate", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_income_tax_rule_RuleYear_IsActive",
                table: "income_tax_rule",
                columns: new[] { "RuleYear", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_income_tax_rule_RuleYear_LevelNo",
                table: "income_tax_rule",
                columns: new[] { "RuleYear", "LevelNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_org_unit_ParentId",
                table: "org_unit",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_output_record_OrgUnitId",
                table: "output_record",
                column: "OrgUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_output_record_ProductId",
                table: "output_record",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_overtime_rate_config_ConfigCode",
                table: "overtime_rate_config",
                column: "ConfigCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_overtime_rate_config_EmployeeTypeId_HolidayType_EffectiveSta~",
                table: "overtime_rate_config",
                columns: new[] { "EmployeeTypeId", "HolidayType", "EffectiveStart" });

            migrationBuilder.CreateIndex(
                name: "IX_payroll_EmployeeId_YearMonth",
                table: "payroll",
                columns: new[] { "EmployeeId", "YearMonth" });

            migrationBuilder.CreateIndex(
                name: "IX_payroll_EmployeeTypeId",
                table: "payroll",
                column: "EmployeeTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_payroll_PayrollRunId_EmployeeId",
                table: "payroll",
                columns: new[] { "PayrollRunId", "EmployeeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payroll_YearMonth_Status",
                table: "payroll",
                columns: new[] { "YearMonth", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_payroll_detail_ComponentCode_ComponentCategory",
                table: "payroll_detail",
                columns: new[] { "ComponentCode", "ComponentCategory" });

            migrationBuilder.CreateIndex(
                name: "IX_payroll_detail_PayrollId_SortOrder",
                table: "payroll_detail",
                columns: new[] { "PayrollId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_payroll_run_ApprovalProcessInstanceId",
                table: "payroll_run",
                column: "ApprovalProcessInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_payroll_run_RunNo",
                table: "payroll_run",
                column: "RunNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payroll_run_RunType_CreatedAt",
                table: "payroll_run",
                columns: new[] { "RunType", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_payroll_run_YearMonth_Status",
                table: "payroll_run",
                columns: new[] { "YearMonth", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_payslip_EmployeeId",
                table: "payslip",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_payslip_SalaryCalculationId",
                table: "payslip",
                column: "SalaryCalculationId");

            migrationBuilder.CreateIndex(
                name: "IX_product_OrgUnitId",
                table: "product",
                column: "OrgUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_salary_adjustment_EmployeeId_YearMonth",
                table: "salary_adjustment",
                columns: new[] { "EmployeeId", "YearMonth" });

            migrationBuilder.CreateIndex(
                name: "IX_salary_adjustment_YearMonth_AdjustmentType",
                table: "salary_adjustment",
                columns: new[] { "YearMonth", "AdjustmentType" });

            migrationBuilder.CreateIndex(
                name: "IX_salary_calculation_EmployeeId",
                table: "salary_calculation",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_salary_rule_EmployeeTypeId_EffectiveStart_EffectiveEnd",
                table: "salary_rule",
                columns: new[] { "EmployeeTypeId", "EffectiveStart", "EffectiveEnd" });

            migrationBuilder.CreateIndex(
                name: "IX_salary_rule_IsActive_EffectiveStart",
                table: "salary_rule",
                columns: new[] { "IsActive", "EffectiveStart" });

            migrationBuilder.CreateIndex(
                name: "IX_salary_rule_RuleCode",
                table: "salary_rule",
                column: "RuleCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sys_config_param_Category_ParamKey",
                table: "sys_config_param",
                columns: new[] { "Category", "ParamKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sys_menu_MenuKey",
                table: "sys_menu",
                column: "MenuKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sys_menu_ParentId",
                table: "sys_menu",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_sys_menu_PermissionCode",
                table: "sys_menu",
                column: "PermissionCode");

            migrationBuilder.CreateIndex(
                name: "IX_sys_post_PostCode",
                table: "sys_post",
                column: "PostCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sys_post_permission_MenuId",
                table: "sys_post_permission",
                column: "MenuId");

            migrationBuilder.CreateIndex(
                name: "IX_sys_post_permission_PostId_MenuId",
                table: "sys_post_permission",
                columns: new[] { "PostId", "MenuId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sys_role_RoleCode",
                table: "sys_role",
                column: "RoleCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sys_role_permission_MenuId",
                table: "sys_role_permission",
                column: "MenuId");

            migrationBuilder.CreateIndex(
                name: "IX_sys_role_permission_RoleId_MenuId",
                table: "sys_role_permission",
                columns: new[] { "RoleId", "MenuId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sys_token_session_UserId",
                table: "sys_token_session",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_sys_user_EmployeeId",
                table: "sys_user",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_sys_user_PostId",
                table: "sys_user",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_sys_user_Username",
                table: "sys_user",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sys_user_role_RoleId",
                table: "sys_user_role",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_sys_user_role_UserId_RoleId",
                table: "sys_user_role",
                columns: new[] { "UserId", "RoleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_third_party_bill_ClientId",
                table: "third_party_bill",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_third_party_bill_detail_ThirdPartyBillId",
                table: "third_party_bill_detail",
                column: "ThirdPartyBillId");

            migrationBuilder.CreateIndex(
                name: "IX_timesheet_ActualOrgUnitId",
                table: "timesheet",
                column: "ActualOrgUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_timesheet_EmployeeId",
                table: "timesheet",
                column: "EmployeeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "attendance");

            migrationBuilder.DropTable(
                name: "employee_payroll_profile");

            migrationBuilder.DropTable(
                name: "expense_application");

            migrationBuilder.DropTable(
                name: "holiday_rule");

            migrationBuilder.DropTable(
                name: "income_tax_rule");

            migrationBuilder.DropTable(
                name: "output_record");

            migrationBuilder.DropTable(
                name: "overtime_rate_config");

            migrationBuilder.DropTable(
                name: "payroll_detail");

            migrationBuilder.DropTable(
                name: "payslip");

            migrationBuilder.DropTable(
                name: "salary_adjustment");

            migrationBuilder.DropTable(
                name: "salary_rule");

            migrationBuilder.DropTable(
                name: "sys_config_param");

            migrationBuilder.DropTable(
                name: "sys_post_permission");

            migrationBuilder.DropTable(
                name: "sys_processed_event");

            migrationBuilder.DropTable(
                name: "sys_role_permission");

            migrationBuilder.DropTable(
                name: "sys_token_session");

            migrationBuilder.DropTable(
                name: "sys_user_role");

            migrationBuilder.DropTable(
                name: "third_party_bill_detail");

            migrationBuilder.DropTable(
                name: "timesheet");

            migrationBuilder.DropTable(
                name: "product");

            migrationBuilder.DropTable(
                name: "payroll");

            migrationBuilder.DropTable(
                name: "salary_calculation");

            migrationBuilder.DropTable(
                name: "sys_menu");

            migrationBuilder.DropTable(
                name: "sys_role");

            migrationBuilder.DropTable(
                name: "sys_user");

            migrationBuilder.DropTable(
                name: "third_party_bill");

            migrationBuilder.DropTable(
                name: "employee_type");

            migrationBuilder.DropTable(
                name: "payroll_run");

            migrationBuilder.DropTable(
                name: "employee");

            migrationBuilder.DropTable(
                name: "sys_post");

            migrationBuilder.DropTable(
                name: "org_unit");
        }
    }
}
