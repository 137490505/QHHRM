using Microsoft.EntityFrameworkCore;
using HRMS.Domain.Entities;

namespace HRMS.Infrastructure.Data;

public class HrmsDbContext : DbContext
{
    public HrmsDbContext(DbContextOptions<HrmsDbContext> options) : base(options) { }

    public DbSet<OrgUnit> OrgUnits => Set<OrgUnit>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<PayrollEmployeeType> EmployeeTypes => Set<PayrollEmployeeType>();
    public DbSet<EmployeePayrollProfile> EmployeePayrollProfiles => Set<EmployeePayrollProfile>();
    public DbSet<Timesheet> Timesheets => Set<Timesheet>();
    public DbSet<Attendance> Attendances => Set<Attendance>();
    public DbSet<SalaryRule> SalaryRules => Set<SalaryRule>();
    public DbSet<OvertimeRateConfig> OvertimeRateConfigs => Set<OvertimeRateConfig>();
    public DbSet<HolidayRule> HolidayRules => Set<HolidayRule>();
    public DbSet<SalaryAdjustment> SalaryAdjustments => Set<SalaryAdjustment>();
    public DbSet<IncomeTaxRule> IncomeTaxRules => Set<IncomeTaxRule>();
    public DbSet<PayrollRun> PayrollRuns => Set<PayrollRun>();
    public DbSet<Payroll> Payrolls => Set<Payroll>();
    public DbSet<PayrollDetail> PayrollDetails => Set<PayrollDetail>();
    public DbSet<SalaryCalculation> SalaryCalculations => Set<SalaryCalculation>();
    public DbSet<Payslip> Payslips => Set<Payslip>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<OutputRecord> OutputRecords => Set<OutputRecord>();
    public DbSet<ThirdPartyBill> ThirdPartyBills => Set<ThirdPartyBill>();
    public DbSet<ThirdPartyBillDetail> ThirdPartyBillDetails => Set<ThirdPartyBillDetail>();
    public DbSet<ExpenseApplication> ExpenseApplications => Set<ExpenseApplication>();
    public DbSet<SysConfigParam> SysConfigParams => Set<SysConfigParam>();
    public DbSet<SysMenu> SysMenus => Set<SysMenu>();
    public DbSet<SysRole> SysRoles => Set<SysRole>();
    public DbSet<SysRolePermission> SysRolePermissions => Set<SysRolePermission>();
    public DbSet<SysUser> SysUsers => Set<SysUser>();
    public DbSet<SysPost> SysPosts => Set<SysPost>();
    public DbSet<SysUserRole> SysUserRoles => Set<SysUserRole>();
    public DbSet<SysPostPermission> SysPostPermissions => Set<SysPostPermission>();
    public DbSet<SysTokenSession> SysTokenSessions => Set<SysTokenSession>();
    public DbSet<SysProcessedEvent> SysProcessedEvents => Set<SysProcessedEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<OrgUnit>(entity =>
        {
            entity.ToTable("org_unit");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.HasOne(e => e.Parent).WithMany(e => e.Children).HasForeignKey(e => e.ParentId);
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("customer");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.ShortName).HasMaxLength(100);
            entity.Property(e => e.ContactPerson).HasMaxLength(100);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.TaxNo).HasMaxLength(100);
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.InvoiceTitle).HasMaxLength(200);
            entity.Property(e => e.Remark).HasMaxLength(1000);
            entity.Property(e => e.CreatedAt).HasColumnType("datetime(6)");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime(6)");
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasIndex(e => new { e.IsActive, e.Name });
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.ToTable("supplier");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.ShortName).HasMaxLength(100);
            entity.Property(e => e.ContactPerson).HasMaxLength(100);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.TaxNo).HasMaxLength(100);
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.BankName).HasMaxLength(100);
            entity.Property(e => e.BankAccount).HasMaxLength(100);
            entity.Property(e => e.Remark).HasMaxLength(1000);
            entity.Property(e => e.CreatedAt).HasColumnType("datetime(6)");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime(6)");
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasIndex(e => new { e.IsActive, e.Name });
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.ToTable("employee");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EmployeeNo).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Gender);
            entity.Property(e => e.IdCard).HasMaxLength(18);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.JobTitle).HasMaxLength(100);
            entity.Property(e => e.Level).HasMaxLength(50);
            entity.Property(e => e.HourlyRate).HasPrecision(10, 2);
            entity.Property(e => e.MonthlySalary).HasPrecision(12, 2);
            entity.Property(e => e.PieceRatePrice).HasPrecision(10, 2);
            entity.Property(e => e.SocialSecurityBase).HasPrecision(12, 2);
            entity.Property(e => e.HousingFundBase).HasPrecision(12, 2);
            entity.Property(e => e.Tags).HasColumnType("json");
            entity.Property(e => e.TrialEndDate).HasColumnType("datetime(6)");
            entity.Property(e => e.TrialDaysRemaining);
            entity.Property(e => e.ProbationDays);
            entity.Property(e => e.ContractType);
            entity.Property(e => e.HireDate).HasColumnType("datetime(6)");
            entity.Property(e => e.ContractStartDate).HasColumnType("datetime(6)");
            entity.Property(e => e.ContractEndDate).HasColumnType("datetime(6)");
            entity.Property(e => e.DismissDate).HasColumnType("datetime(6)");
            entity.Property(e => e.DismissReason).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasColumnType("datetime(6)");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime(6)");
            entity.HasIndex(e => e.EmployeeNo).IsUnique();
            entity.HasIndex(e => e.OrgUnitId);
            entity.HasIndex(e => e.ThirdPartyCompanyId);
            entity.HasOne(e => e.OrgUnit).WithMany().HasForeignKey(e => e.OrgUnitId);
            entity.HasOne(e => e.ThirdPartyCompany).WithMany().HasForeignKey(e => e.ThirdPartyCompanyId);
        });

        modelBuilder.Entity<PayrollEmployeeType>(entity =>
        {
            entity.ToTable("employee_type");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TypeCode).HasMaxLength(64).IsRequired();
            entity.Property(e => e.TypeName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.SalaryMode).HasMaxLength(32).IsRequired();
            entity.Property(e => e.Remark).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasColumnType("datetime(6)");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime(6)");
            entity.HasIndex(e => e.TypeCode).IsUnique();
            entity.HasIndex(e => e.TypeName).IsUnique();
            entity.HasIndex(e => new { e.IsActive, e.SortOrder });
        });

        modelBuilder.Entity<EmployeePayrollProfile>(entity =>
        {
            entity.ToTable("employee_payroll_profile");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ShiftType).HasMaxLength(32);
            entity.Property(e => e.BankAccount).HasMaxLength(64);
            entity.Property(e => e.PayrollStatus).HasMaxLength(32).IsRequired();
            entity.Property(e => e.Remark).HasMaxLength(500);
            entity.Property(e => e.JoinPayrollDate).HasColumnType("date");
            entity.Property(e => e.LeavePayrollDate).HasColumnType("date");
            entity.Property(e => e.CreatedAt).HasColumnType("datetime(6)");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime(6)");
            entity.HasIndex(e => e.EmployeeId).IsUnique();
            entity.HasIndex(e => new { e.EmployeeTypeId, e.PayrollStatus });
            entity.HasOne(e => e.Employee).WithMany().HasForeignKey(e => e.EmployeeId);
            entity.HasOne(e => e.EmployeeType).WithMany().HasForeignKey(e => e.EmployeeTypeId);
        });

        modelBuilder.Entity<SalaryRule>(entity =>
        {
            entity.ToTable("salary_rule");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RuleCode).HasMaxLength(64).IsRequired();
            entity.Property(e => e.RuleName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Remark).HasMaxLength(500);
            entity.Property(e => e.EffectiveStart).HasColumnType("date");
            entity.Property(e => e.EffectiveEnd).HasColumnType("date");
            entity.Property(e => e.FixedSalary).HasPrecision(12, 2);
            entity.Property(e => e.HourlyRate).HasPrecision(10, 2);
            entity.Property(e => e.PieceworkUnitPrice).HasPrecision(10, 2);
            entity.Property(e => e.BaseSalaryForPiecework).HasPrecision(12, 2);
            entity.Property(e => e.MealSubsidyPerDay).HasPrecision(10, 2);
            entity.Property(e => e.NightSubsidyPerDay).HasPrecision(10, 2);
            entity.Property(e => e.PerformanceBase).HasPrecision(12, 2);
            entity.Property(e => e.DefaultOvertimeMultiplier).HasPrecision(6, 2);
            entity.Property(e => e.CreatedAt).HasColumnType("datetime(6)");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime(6)");
            entity.HasIndex(e => e.RuleCode).IsUnique();
            entity.HasIndex(e => new { e.EmployeeTypeId, e.EffectiveStart, e.EffectiveEnd });
            entity.HasIndex(e => new { e.IsActive, e.EffectiveStart });
            entity.HasOne(e => e.EmployeeType).WithMany().HasForeignKey(e => e.EmployeeTypeId);
        });

        modelBuilder.Entity<OvertimeRateConfig>(entity =>
        {
            entity.ToTable("overtime_rate_config");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ConfigCode).HasMaxLength(64).IsRequired();
            entity.Property(e => e.HolidayType).HasMaxLength(32).IsRequired();
            entity.Property(e => e.Multiplier).HasPrecision(6, 2);
            entity.Property(e => e.EffectiveStart).HasColumnType("date");
            entity.Property(e => e.EffectiveEnd).HasColumnType("date");
            entity.Property(e => e.Remark).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasColumnType("datetime(6)");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime(6)");
            entity.HasIndex(e => e.ConfigCode).IsUnique();
            entity.HasIndex(e => new { e.EmployeeTypeId, e.HolidayType, e.EffectiveStart });
            entity.HasOne(e => e.EmployeeType).WithMany().HasForeignKey(e => e.EmployeeTypeId);
        });

        modelBuilder.Entity<HolidayRule>(entity =>
        {
            entity.ToTable("holiday_rule");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.HolidayCode).HasMaxLength(64).IsRequired();
            entity.Property(e => e.HolidayName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.HolidayType).HasMaxLength(32).IsRequired();
            entity.Property(e => e.HolidayDate).HasColumnType("date");
            entity.Property(e => e.OvertimeMultiplier).HasPrecision(6, 2);
            entity.Property(e => e.Remark).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasColumnType("datetime(6)");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime(6)");
            entity.HasIndex(e => e.HolidayCode).IsUnique();
            entity.HasIndex(e => new { e.HolidayDate, e.HolidayType }).IsUnique();
            entity.HasIndex(e => new { e.HolidayDate, e.IsActive });
        });

        modelBuilder.Entity<Timesheet>(entity =>
        {
            entity.ToTable("timesheet");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.WorkingHours).HasPrecision(5, 2);
            entity.Property(e => e.OvertimeHours).HasPrecision(5, 2);
            entity.HasOne(e => e.Employee).WithMany().HasForeignKey(e => e.EmployeeId);
            entity.HasOne(e => e.ActualOrgUnit).WithMany().HasForeignKey(e => e.ActualOrgUnitId);
        });

        modelBuilder.Entity<Attendance>(entity =>
        {
            entity.ToTable("attendance");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.WorkDate).HasColumnType("date");
            entity.Property(e => e.NormalHours).HasPrecision(6, 2);
            entity.Property(e => e.OvertimeHours).HasPrecision(6, 2);
            entity.Property(e => e.OvertimeType).HasMaxLength(32);
            entity.Property(e => e.PieceworkQty).HasPrecision(12, 2);
            entity.Property(e => e.ShiftType).HasMaxLength(32);
            entity.Property(e => e.AttendanceSource).HasMaxLength(32).IsRequired();
            entity.Property(e => e.SourceRecordId).HasMaxLength(128);
            entity.Property(e => e.Status).HasMaxLength(32).IsRequired();
            entity.Property(e => e.Remark).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasColumnType("datetime(6)");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime(6)");
            entity.HasIndex(e => new { e.EmployeeId, e.WorkDate }).IsUnique();
            entity.HasIndex(e => new { e.WorkDate, e.Status });
            entity.HasIndex(e => new { e.AttendanceSource, e.SourceRecordId });
            entity.HasOne(e => e.Employee).WithMany().HasForeignKey(e => e.EmployeeId);
        });

        modelBuilder.Entity<SalaryAdjustment>(entity =>
        {
            entity.ToTable("salary_adjustment");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.YearMonth).HasMaxLength(7).IsRequired();
            entity.Property(e => e.AdjustmentType).HasMaxLength(64).IsRequired();
            entity.Property(e => e.Amount).HasPrecision(12, 2);
            entity.Property(e => e.SourceType).HasMaxLength(32).IsRequired();
            entity.Property(e => e.SourceId).HasMaxLength(64);
            entity.Property(e => e.Remark).HasMaxLength(500);
            entity.Property(e => e.CreatedBy).HasMaxLength(64);
            entity.Property(e => e.UpdatedBy).HasMaxLength(64);
            entity.Property(e => e.CreatedAt).HasColumnType("datetime(6)");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime(6)");
            entity.HasIndex(e => new { e.EmployeeId, e.YearMonth });
            entity.HasIndex(e => new { e.YearMonth, e.AdjustmentType });
            entity.HasOne(e => e.Employee).WithMany().HasForeignKey(e => e.EmployeeId);
        });

        modelBuilder.Entity<IncomeTaxRule>(entity =>
        {
            entity.ToTable("income_tax_rule");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MinTaxableAmount).HasPrecision(12, 2);
            entity.Property(e => e.MaxTaxableAmount).HasPrecision(12, 2);
            entity.Property(e => e.TaxRate).HasPrecision(6, 4);
            entity.Property(e => e.QuickDeduction).HasPrecision(12, 2);
            entity.Property(e => e.ThresholdAmount).HasPrecision(12, 2);
            entity.Property(e => e.Remark).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasColumnType("datetime(6)");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime(6)");
            entity.HasIndex(e => new { e.RuleYear, e.LevelNo }).IsUnique();
            entity.HasIndex(e => new { e.RuleYear, e.IsActive });
        });

        modelBuilder.Entity<PayrollRun>(entity =>
        {
            entity.ToTable("payroll_run");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RunNo).HasMaxLength(64).IsRequired();
            entity.Property(e => e.YearMonth).HasMaxLength(7).IsRequired();
            entity.Property(e => e.RunType).HasMaxLength(32).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(32).IsRequired();
            entity.Property(e => e.EmployeeScopeJson).HasColumnType("json");
            entity.Property(e => e.RuleSnapshotJson).HasColumnType("json");
            entity.Property(e => e.TriggeredBy).HasMaxLength(64);
            entity.Property(e => e.ApprovalProcessCode).HasMaxLength(64);
            entity.Property(e => e.ApprovalProcessInstanceId).HasMaxLength(64);
            entity.Property(e => e.ApprovalRequestId).HasMaxLength(128);
            entity.Property(e => e.ApprovedBy).HasMaxLength(64);
            entity.Property(e => e.Remark).HasMaxLength(1000);
            entity.Property(e => e.ApprovalSubmittedAt).HasColumnType("datetime(6)");
            entity.Property(e => e.ApprovedAt).HasColumnType("datetime(6)");
            entity.Property(e => e.StartedAt).HasColumnType("datetime(6)");
            entity.Property(e => e.FinishedAt).HasColumnType("datetime(6)");
            entity.Property(e => e.CreatedAt).HasColumnType("datetime(6)");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime(6)");
            entity.HasIndex(e => e.RunNo).IsUnique();
            entity.HasIndex(e => new { e.YearMonth, e.Status });
            entity.HasIndex(e => new { e.RunType, e.CreatedAt });
            entity.HasIndex(e => e.ApprovalProcessInstanceId);
        });

        modelBuilder.Entity<Payroll>(entity =>
        {
            entity.ToTable("payroll");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.YearMonth).HasMaxLength(7).IsRequired();
            entity.Property(e => e.EmployeeNoSnapshot).HasMaxLength(64).IsRequired();
            entity.Property(e => e.EmployeeNameSnapshot).HasMaxLength(100).IsRequired();
            entity.Property(e => e.SalaryModeSnapshot).HasMaxLength(32).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(32).IsRequired();
            foreach (var prop in typeof(Payroll).GetProperties().Where(p => p.PropertyType == typeof(decimal)))
            {
                entity.Property(prop.Name).HasPrecision(12, 2);
            }
            entity.Property(e => e.CalculatedAt).HasColumnType("datetime(6)");
            entity.Property(e => e.PaidAt).HasColumnType("datetime(6)");
            entity.Property(e => e.CreatedAt).HasColumnType("datetime(6)");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime(6)");
            entity.HasIndex(e => new { e.PayrollRunId, e.EmployeeId }).IsUnique();
            entity.HasIndex(e => new { e.EmployeeId, e.YearMonth });
            entity.HasIndex(e => new { e.YearMonth, e.Status });
            entity.HasOne(e => e.PayrollRun).WithMany(e => e.Payrolls).HasForeignKey(e => e.PayrollRunId);
            entity.HasOne(e => e.Employee).WithMany().HasForeignKey(e => e.EmployeeId);
            entity.HasOne(e => e.EmployeeType).WithMany().HasForeignKey(e => e.EmployeeTypeId);
        });

        modelBuilder.Entity<PayrollDetail>(entity =>
        {
            entity.ToTable("payroll_detail");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ComponentCode).HasMaxLength(64).IsRequired();
            entity.Property(e => e.ComponentName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.ComponentCategory).HasMaxLength(32).IsRequired();
            entity.Property(e => e.Amount).HasPrecision(12, 2);
            entity.Property(e => e.Quantity).HasPrecision(12, 2);
            entity.Property(e => e.UnitPrice).HasPrecision(12, 2);
            entity.Property(e => e.SourceType).HasMaxLength(32).IsRequired();
            entity.Property(e => e.SourceId).HasMaxLength(64);
            entity.Property(e => e.Remark).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasColumnType("datetime(6)");
            entity.HasIndex(e => new { e.PayrollId, e.SortOrder });
            entity.HasIndex(e => new { e.ComponentCode, e.ComponentCategory });
            entity.HasOne(e => e.Payroll).WithMany(e => e.Details).HasForeignKey(e => e.PayrollId);
        });

        modelBuilder.Entity<SalaryCalculation>(entity =>
        {
            entity.ToTable("salary_calculations");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RegularHours).HasPrecision(8, 2);
            entity.Property(e => e.OvertimeHours).HasPrecision(8, 2);
            foreach (var prop in typeof(SalaryCalculation).GetProperties().Where(p => p.PropertyType == typeof(decimal)))
            {
                entity.Property(prop.Name).HasPrecision(12, 2);
            }
            entity.HasOne(e => e.Employee).WithMany().HasForeignKey(e => e.EmployeeId);
        });

        modelBuilder.Entity<Payslip>(entity =>
        {
            entity.ToTable("payslip");
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.SalaryCalculation).WithMany().HasForeignKey(e => e.SalaryCalculationId);
            entity.HasOne(e => e.Employee).WithMany().HasForeignKey(e => e.EmployeeId);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("product");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Unit).HasMaxLength(20);
            entity.Property(e => e.ClientUnitPrice).HasPrecision(10, 2);
            entity.Property(e => e.EmployeeUnitPrice).HasPrecision(10, 2);
            entity.HasOne(e => e.OrgUnit).WithMany().HasForeignKey(e => e.OrgUnitId);
        });

        modelBuilder.Entity<OutputRecord>(entity =>
        {
            entity.ToTable("output_record");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.QualifiedQuantity).HasPrecision(10, 2);
            entity.HasOne(e => e.Product).WithMany().HasForeignKey(e => e.ProductId);
            entity.HasOne(e => e.OrgUnit).WithMany().HasForeignKey(e => e.OrgUnitId);
        });

        modelBuilder.Entity<ThirdPartyBill>(entity =>
        {
            entity.ToTable("third_party_bill");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.BillNo).HasMaxLength(50).IsRequired();
            entity.Property(e => e.ManagementFeeType).HasMaxLength(50);
            foreach (var prop in typeof(ThirdPartyBill).GetProperties().Where(p => p.PropertyType == typeof(decimal)))
            {
                entity.Property(prop.Name).HasPrecision(12, 2);
            }
            entity.HasOne(e => e.Client).WithMany().HasForeignKey(e => e.ClientId);
        });

        modelBuilder.Entity<ThirdPartyBillDetail>(entity =>
        {
            entity.ToTable("third_party_bill_detail");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Tag).HasMaxLength(100);
            entity.Property(e => e.TotalWages).HasPrecision(12, 2);
            entity.Property(e => e.TotalManagementFee).HasPrecision(12, 2);
            entity.Property(e => e.TotalHours).HasPrecision(10, 2);
            entity.HasOne(e => e.ThirdPartyBill).WithMany(e => e.Details).HasForeignKey(e => e.ThirdPartyBillId);
        });

        modelBuilder.Entity<ExpenseApplication>(entity =>
        {
            entity.ToTable("expense_application");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ApplicationNo).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Amount).HasPrecision(12, 2);
            entity.HasOne(e => e.OrgUnit).WithMany().HasForeignKey(e => e.OrgUnitId);
        });

        modelBuilder.Entity<SysConfigParam>(entity =>
        {
            entity.ToTable("sys_config_param");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Category).HasMaxLength(50).IsRequired();
            entity.Property(e => e.ParamKey).HasMaxLength(100).IsRequired();
            entity.Property(e => e.ParamValue).HasColumnType("longtext");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.ChangeReason).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasColumnType("datetime(6)");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime(6)");
            entity.Property(e => e.ScheduledTakeEffectDate).HasColumnType("datetime(6)");
            entity.HasIndex(e => new { e.Category, e.ParamKey }).IsUnique();
        });

        modelBuilder.Entity<SysMenu>(entity =>
        {
            entity.ToTable("sys_menu");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MenuKey).HasMaxLength(100).IsRequired();
            entity.Property(e => e.MenuName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.MenuType).HasMaxLength(20).IsRequired();
            entity.Property(e => e.RoutePath).HasMaxLength(200);
            entity.Property(e => e.ComponentPath).HasMaxLength(200);
            entity.Property(e => e.Icon).HasMaxLength(50);
            entity.Property(e => e.PermissionCode).HasMaxLength(100);
            entity.HasIndex(e => e.MenuKey).IsUnique();
            entity.HasIndex(e => e.PermissionCode);
            entity.HasOne(e => e.Parent).WithMany(e => e.Children).HasForeignKey(e => e.ParentId);
        });

        modelBuilder.Entity<SysRole>(entity =>
        {
            entity.ToTable("sys_role");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RoleCode).HasMaxLength(100).IsRequired();
            entity.Property(e => e.RoleName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.HasIndex(e => e.RoleCode).IsUnique();
        });

        modelBuilder.Entity<SysRolePermission>(entity =>
        {
            entity.ToTable("sys_role_permission");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PermissionCode).HasMaxLength(100).IsRequired();
            entity.Property(e => e.PermissionType).HasMaxLength(50);
            entity.HasOne(e => e.Role).WithMany(e => e.Permissions).HasForeignKey(e => e.RoleId);
            entity.HasOne(e => e.Menu).WithMany().HasForeignKey(e => e.MenuId);
            entity.HasIndex(e => new { e.RoleId, e.MenuId }).IsUnique();
        });

        modelBuilder.Entity<SysUser>(entity =>
        {
            entity.ToTable("sys_user");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Username).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Password).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasOne(e => e.Employee).WithMany().HasForeignKey(e => e.EmployeeId);
            entity.HasOne(e => e.Post).WithMany(e => e.Users).HasForeignKey(e => e.PostId);
        });

        modelBuilder.Entity<SysPost>(entity =>
        {
            entity.ToTable("sys_post");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PostCode).HasMaxLength(100).IsRequired();
            entity.Property(e => e.PostName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.HasIndex(e => e.PostCode).IsUnique();
        });

        modelBuilder.Entity<SysUserRole>(entity =>
        {
            entity.ToTable("sys_user_role");
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User).WithMany(e => e.UserRoles).HasForeignKey(e => e.UserId);
            entity.HasOne(e => e.Role).WithMany(e => e.UserRoles).HasForeignKey(e => e.RoleId);
            entity.HasIndex(e => new { e.UserId, e.RoleId }).IsUnique();
        });

        modelBuilder.Entity<SysPostPermission>(entity =>
        {
            entity.ToTable("sys_post_permission");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PermissionCode).HasMaxLength(100).IsRequired();
            entity.Property(e => e.PermissionType).HasMaxLength(50).IsRequired();
            entity.HasOne(e => e.Post).WithMany(e => e.Permissions).HasForeignKey(e => e.PostId);
            entity.HasOne(e => e.Menu).WithMany().HasForeignKey(e => e.MenuId);
            entity.HasIndex(e => new { e.PostId, e.MenuId }).IsUnique();
        });

        modelBuilder.Entity<SysTokenSession>(entity =>
        {
            entity.ToTable("sys_token_session");
            entity.HasKey(e => e.Token);
            entity.Property(e => e.Token).HasMaxLength(128);
            entity.Property(e => e.PermissionsJson).HasColumnType("json");
            entity.Property(e => e.CreatedAt).HasColumnType("datetime(6)");
            entity.Property(e => e.ExpiresAt).HasColumnType("datetime(6)");
            entity.HasIndex(e => e.UserId);
        });

        modelBuilder.Entity<SysProcessedEvent>(entity =>
        {
            entity.ToTable("sys_processed_event");
            entity.HasKey(e => e.EventId);
            entity.Property(e => e.EventId).HasMaxLength(128);
            entity.Property(e => e.ProcessedTime).HasColumnType("datetime(6)");
        });
    }
}
