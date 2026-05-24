using System.Text.Json;
using HRMS.Application.DTOs;
using HRMS.Domain.Entities;
using HRMS.Domain.Interfaces;

namespace HRMS.Application.Services;

public class PayrollCalculationService
{
    private readonly IEmployeePayrollProfileRepository _profileRepository;
    private readonly ISalaryRuleRepository _salaryRuleRepository;
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly ISalaryAdjustmentRepository _adjustmentRepository;
    private readonly IOvertimeRateConfigRepository _overtimeRateConfigRepository;
    private readonly IHolidayRuleRepository _holidayRuleRepository;
    private readonly IIncomeTaxRuleRepository _incomeTaxRuleRepository;
    private readonly IPayrollRunRepository _payrollRunRepository;
    private readonly IPayrollRepository _payrollRepository;

    public PayrollCalculationService(
        IEmployeePayrollProfileRepository profileRepository,
        ISalaryRuleRepository salaryRuleRepository,
        IAttendanceRepository attendanceRepository,
        ISalaryAdjustmentRepository adjustmentRepository,
        IOvertimeRateConfigRepository overtimeRateConfigRepository,
        IHolidayRuleRepository holidayRuleRepository,
        IIncomeTaxRuleRepository incomeTaxRuleRepository,
        IPayrollRunRepository payrollRunRepository,
        IPayrollRepository payrollRepository)
    {
        _profileRepository = profileRepository;
        _salaryRuleRepository = salaryRuleRepository;
        _attendanceRepository = attendanceRepository;
        _adjustmentRepository = adjustmentRepository;
        _overtimeRateConfigRepository = overtimeRateConfigRepository;
        _holidayRuleRepository = holidayRuleRepository;
        _incomeTaxRuleRepository = incomeTaxRuleRepository;
        _payrollRunRepository = payrollRunRepository;
        _payrollRepository = payrollRepository;
    }

    public async Task<PayrollRunDto> TrialAsync(ExecutePayrollRunDto dto)
    {
        var context = await BuildPayrollContextAsync(dto, isTrial: true);
        var payrolls = await CalculatePayrollsAsync(context, persistPayrollId: false);

        return MapRunDto(new PayrollRun
        {
            Id = Guid.Empty,
            RunNo = BuildRunNo("Trial"),
            YearMonth = context.YearMonth,
            RunType = "Trial",
            Status = "Draft",
            Remark = dto.Remark,
            StartedAt = context.StartedAt,
            FinishedAt = DateTime.UtcNow
        }, payrolls);
    }

    public async Task<PayrollRunDto> CalculateAsync(ExecutePayrollRunDto dto)
    {
        var context = await BuildPayrollContextAsync(dto, isTrial: false);
        var run = new PayrollRun
        {
            RunNo = BuildRunNo("Monthly"),
            YearMonth = context.YearMonth,
            RunType = "Monthly",
            Status = "Draft",
            EmployeeScopeJson = JsonSerializer.Serialize(new
            {
                dto.EmployeeIds,
                dto.OrgUnitIds
            }),
            RuleSnapshotJson = JsonSerializer.Serialize(new
            {
                GeneratedAt = DateTime.UtcNow,
                YearMonth = context.YearMonth
            }),
            StartedAt = context.StartedAt,
            Remark = dto.Remark
        };

        run = await _payrollRunRepository.CreateAsync(run);
        var payrolls = await CalculatePayrollsAsync(context, persistPayrollId: true, payrollRunId: run.Id);

        await _payrollRepository.CreateRangeAsync(payrolls);

        run.Status = "Calculated";
        run.FinishedAt = DateTime.UtcNow;
        await _payrollRunRepository.UpdateAsync(run);

        run.Payrolls = payrolls;
        return MapRunDto(run, payrolls);
    }

    public async Task<IEnumerable<PayrollRunDto>> GetRunsAsync(string yearMonth)
    {
        ValidateYearMonth(yearMonth);
        var runs = await _payrollRunRepository.GetByMonthAsync(yearMonth);
        return runs.Select(run => MapRunDto(run, run.Payrolls));
    }

    public async Task<PayrollRunDto?> GetRunAsync(Guid runId)
    {
        var run = await _payrollRunRepository.GetByIdAsync(runId);
        return run == null ? null : MapRunDto(run, run.Payrolls);
    }

    public async Task<PayrollEmployeeResultDto?> GetRunEmployeeAsync(Guid runId, Guid employeeId)
    {
        var payrolls = await _payrollRepository.GetByRunIdAsync(runId);
        var payroll = payrolls.FirstOrDefault(x => x.EmployeeId == employeeId);
        return payroll == null ? null : MapPayrollDto(payroll);
    }

    public async Task<PayrollRunDto?> RollbackAsync(Guid runId, string? remark)
    {
        var run = await _payrollRunRepository.GetByIdAsync(runId);
        if (run == null)
        {
            return null;
        }

        ValidateRollbackStatus(run.Status);

        var payrolls = await _payrollRepository.GetByRunIdAsync(runId);
        foreach (var payroll in payrolls)
        {
            payroll.Status = "RolledBack";
            await _payrollRepository.UpdateAsync(payroll);
        }

        run.Status = "RolledBack";
        run.Remark = string.IsNullOrWhiteSpace(remark)
            ? run.Remark
            : $"{run.Remark}\n[Rollback] {remark}".Trim();
        run.UpdatedAt = DateTime.UtcNow;
        await _payrollRunRepository.UpdateAsync(run);

        var refreshedRun = await _payrollRunRepository.GetByIdAsync(runId);
        return refreshedRun == null ? null : MapRunDto(refreshedRun, refreshedRun.Payrolls);
    }

    public async Task<PayrollRunDto?> ApproveAsync(Guid runId, string? approvedBy, string? remark)
    {
        var run = await _payrollRunRepository.GetByIdAsync(runId);
        if (run == null)
        {
            return null;
        }

        if (!string.Equals(run.Status, "Calculated", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("只有已核算批次才能审核");
        }

        var payrolls = await _payrollRepository.GetByRunIdAsync(runId);
        foreach (var payroll in payrolls)
        {
            payroll.Status = "Approved";
            await _payrollRepository.UpdateAsync(payroll);
        }

        run.Status = "Approved";
        run.ApprovedBy = approvedBy;
        run.ApprovedAt = DateTime.UtcNow;
        run.Remark = AppendRemark(run.Remark, "Approve", remark);
        await _payrollRunRepository.UpdateAsync(run);

        var refreshedRun = await _payrollRunRepository.GetByIdAsync(runId);
        return refreshedRun == null ? null : MapRunDto(refreshedRun, refreshedRun.Payrolls);
    }

    public async Task<PayrollRunDto?> ApproveByWorkflowAsync(
        Guid runId,
        string processInstanceId,
        string? operatorId,
        string? operatorName,
        string? comment)
    {
        var run = await _payrollRunRepository.GetByIdAsync(runId);
        if (run == null)
        {
            return null;
        }

        ValidateApprovalProcessInstance(run, processInstanceId);
        if (string.Equals(run.Status, "Approved", StringComparison.OrdinalIgnoreCase))
        {
            return MapRunDto(run, run.Payrolls);
        }

        if (!string.Equals(run.Status, "Calculated", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("当前薪资批次状态不允许处理审批通过回调");
        }

        return await ApproveAsync(runId, ResolveWorkflowApprovedBy(operatorId, operatorName), comment);
    }

    public async Task<PayrollRunDto?> MarkApprovalSubmittedAsync(
        Guid runId,
        string processCode,
        string processInstanceId,
        string requestId,
        string? submittedBy,
        string? remark)
    {
        var run = await _payrollRunRepository.GetByIdAsync(runId);
        if (run == null)
        {
            return null;
        }

        if (!string.Equals(run.Status, "Calculated", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("只有已核算批次才能提交审批流程");
        }

        if (!string.IsNullOrWhiteSpace(run.ApprovalProcessInstanceId))
        {
            throw new InvalidOperationException("该薪资批次已提交审批流程");
        }

        run.ApprovalProcessCode = processCode;
        run.ApprovalProcessInstanceId = processInstanceId;
        run.ApprovalRequestId = requestId;
        run.ApprovalSubmittedAt = DateTime.UtcNow;
        run.Remark = AppendRemark(run.Remark, "SubmitApproval", remark, submittedBy);
        await _payrollRunRepository.UpdateAsync(run);

        var refreshedRun = await _payrollRunRepository.GetByIdAsync(runId);
        return refreshedRun == null ? null : MapRunDto(refreshedRun, refreshedRun.Payrolls);
    }

    public async Task<PayrollRunDto?> RejectApprovalAsync(
        Guid runId,
        string processInstanceId,
        string? operatorId,
        string? operatorName,
        string? comment)
    {
        var run = await _payrollRunRepository.GetByIdAsync(runId);
        if (run == null)
        {
            return null;
        }

        ValidateApprovalProcessInstance(run, processInstanceId);
        if (string.Equals(run.Status, "ApprovalRejected", StringComparison.OrdinalIgnoreCase))
        {
            return MapRunDto(run, run.Payrolls);
        }

        if (!string.Equals(run.Status, "Calculated", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("当前薪资批次状态不允许处理审批驳回回调");
        }

        var payrolls = await _payrollRepository.GetByRunIdAsync(runId);
        foreach (var payroll in payrolls)
        {
            payroll.Status = "ApprovalRejected";
            await _payrollRepository.UpdateAsync(payroll);
        }

        run.Status = "ApprovalRejected";
        run.Remark = AppendRemark(run.Remark, "ApprovalReject", comment, ResolveWorkflowActor(operatorId, operatorName));
        await _payrollRunRepository.UpdateAsync(run);

        var refreshedRun = await _payrollRunRepository.GetByIdAsync(runId);
        return refreshedRun == null ? null : MapRunDto(refreshedRun, refreshedRun.Payrolls);
    }

    public async Task<PayrollRunDto?> PayAsync(Guid runId, string? paidBy, string? remark)
    {
        var run = await _payrollRunRepository.GetByIdAsync(runId);
        if (run == null)
        {
            return null;
        }

        if (!string.Equals(run.Status, "Approved", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("只有已审核批次才能发放");
        }

        var payrolls = await _payrollRepository.GetByRunIdAsync(runId);
        var paidAt = DateTime.UtcNow;
        foreach (var payroll in payrolls)
        {
            payroll.Status = "Paid";
            payroll.PaidAt = paidAt;
            await _payrollRepository.UpdateAsync(payroll);
        }

        run.Status = "Paid";
        run.Remark = AppendRemark(run.Remark, "Pay", remark, paidBy);
        await _payrollRunRepository.UpdateAsync(run);

        var refreshedRun = await _payrollRunRepository.GetByIdAsync(runId);
        return refreshedRun == null ? null : MapRunDto(refreshedRun, refreshedRun.Payrolls);
    }

    private async Task<PayrollExecutionContext> BuildPayrollContextAsync(ExecutePayrollRunDto dto, bool isTrial)
    {
        ValidateYearMonth(dto.YearMonth);
        var (year, month) = ParseYearMonth(dto.YearMonth);
        var monthDate = new DateTime(year, month, 1);
        var profiles = (await _profileRepository.GetAllAsync())
            .Where(IsProfileActive)
            .Where(profile => profile.Employee != null && profile.EmployeeType != null)
            .Where(profile => dto.EmployeeIds == null || dto.EmployeeIds.Count == 0 || dto.EmployeeIds.Contains(profile.EmployeeId))
            .Where(profile => dto.OrgUnitIds == null || dto.OrgUnitIds.Count == 0 || dto.OrgUnitIds.Contains(profile.Employee!.OrgUnitId))
            .ToList();

        if (profiles.Count == 0)
        {
            throw new InvalidOperationException("当前筛选条件下没有可参与算薪的员工");
        }

        if (!isTrial)
        {
            var existingPayrolls = await _payrollRepository.GetByMonthAsync(dto.YearMonth);
            var duplicatedEmployees = existingPayrolls
                .Where(x => !string.Equals(x.Status, "RolledBack", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(x.PayrollRun?.Status, "RolledBack", StringComparison.OrdinalIgnoreCase))
                .Select(x => x.EmployeeId)
                .ToHashSet();

            var conflictEmployee = profiles.FirstOrDefault(x => duplicatedEmployees.Contains(x.EmployeeId));
            if (conflictEmployee != null)
            {
                throw new InvalidOperationException($"员工 {conflictEmployee.Employee?.Name} 在 {dto.YearMonth} 已存在正式工资单");
            }
        }

        return new PayrollExecutionContext
        {
            YearMonth = dto.YearMonth,
            Year = year,
            Month = month,
            CalculationDate = monthDate,
            Profiles = profiles,
            StartedAt = DateTime.UtcNow
        };
    }

    private async Task<List<Payroll>> CalculatePayrollsAsync(
        PayrollExecutionContext context,
        bool persistPayrollId,
        Guid? payrollRunId = null)
    {
        var payrolls = new List<Payroll>();
        foreach (var profile in context.Profiles)
        {
            var employee = profile.Employee!;
            var employeeType = profile.EmployeeType!;
            var salaryRule = await _salaryRuleRepository.GetActiveRuleAsync(profile.EmployeeTypeId, context.CalculationDate)
                ?? throw new InvalidOperationException($"员工 {employee.Name} 缺少生效薪资规则");

            var attendances = (await _attendanceRepository.GetByEmployeeAndMonthAsync(employee.Id, context.Year, context.Month)).ToList();
            var adjustments = (await _adjustmentRepository.GetByEmployeeAndMonthAsync(employee.Id, context.YearMonth)).ToList();

            var payroll = new Payroll
            {
                Id = persistPayrollId ? Guid.NewGuid() : Guid.Empty,
                PayrollRunId = payrollRunId ?? Guid.Empty,
                EmployeeId = employee.Id,
                EmployeeTypeId = employeeType.Id,
                YearMonth = context.YearMonth,
                EmployeeNoSnapshot = employee.EmployeeNo,
                EmployeeNameSnapshot = employee.Name,
                OrgUnitIdSnapshot = employee.OrgUnitId,
                SalaryModeSnapshot = employeeType.SalaryMode,
                Status = persistPayrollId ? "Calculated" : "Draft",
                CalculatedAt = DateTime.UtcNow
            };

            await FillAmountsAsync(payroll, profile, salaryRule, attendances, adjustments, context.CalculationDate);
            payrolls.Add(payroll);
        }

        return payrolls;
    }

    private async Task FillAmountsAsync(
        Payroll payroll,
        EmployeePayrollProfile profile,
        SalaryRule salaryRule,
        List<Attendance> attendances,
        List<SalaryAdjustment> adjustments,
        DateTime calculationDate)
    {
        var employeeType = profile.EmployeeType!;
        var normalHours = attendances.Sum(x => x.NormalHours);
        var pieceworkQty = attendances.Sum(x => x.PieceworkQty);
        var mealDays = attendances.Count(x => x.NormalHours + x.OvertimeHours > 0);
        var nightDays = attendances.Count(x => x.IsNightShift);

        payroll.OvertimeWage = await CalculateOvertimeWageAsync(profile.EmployeeTypeId, salaryRule, attendances);
        payroll.MealSubsidy = employeeType.HasMealSubsidy ? mealDays * salaryRule.MealSubsidyPerDay : 0;
        payroll.NightSubsidy = employeeType.HasNightSubsidy ? nightDays * salaryRule.NightSubsidyPerDay : 0;
        payroll.PerformanceBonus = employeeType.HasPerformance ? salaryRule.PerformanceBase ?? 0 : 0;

        switch (NormalizeSalaryMode(employeeType.SalaryMode))
        {
            case "Management":
            case "Fixed":
                payroll.FixedSalary = salaryRule.FixedSalary ?? 0;
                break;
            case "Hourly":
                payroll.NormalWage = normalHours * (salaryRule.HourlyRate ?? 0);
                break;
            case "Piecework":
                payroll.PieceworkWage = pieceworkQty * (salaryRule.PieceworkUnitPrice ?? 0);
                break;
            case "BasePlusPiecework":
                payroll.BaseSalary = salaryRule.BaseSalaryForPiecework ?? 0;
                payroll.PieceworkWage = pieceworkQty * (salaryRule.PieceworkUnitPrice ?? 0);
                break;
            case "ThirdParty":
                if (salaryRule.FixedSalary.HasValue && salaryRule.FixedSalary.Value > 0)
                {
                    payroll.FixedSalary = salaryRule.FixedSalary.Value;
                }
                else if (salaryRule.HourlyRate.HasValue && salaryRule.HourlyRate.Value > 0)
                {
                    payroll.NormalWage = normalHours * salaryRule.HourlyRate.Value;
                }
                else
                {
                    payroll.PieceworkWage = pieceworkQty * (salaryRule.PieceworkUnitPrice ?? 0);
                }
                break;
            default:
                payroll.FixedSalary = salaryRule.FixedSalary ?? 0;
                break;
        }

        ApplyAdjustments(payroll, adjustments);

        var taxableBase = Math.Max(
            0,
            payroll.FixedSalary + payroll.BaseSalary + payroll.NormalWage + payroll.OvertimeWage + payroll.PieceworkWage
                + payroll.MealSubsidy + payroll.NightSubsidy + payroll.PerformanceBonus + payroll.OtherAllowance
                - payroll.SocialSecurityEmployee - payroll.ProvidentFundEmployee);

        if (payroll.IncomeTax == 0)
        {
            payroll.IncomeTax = await CalculateIncomeTaxAsync(calculationDate.Year, taxableBase);
        }

        payroll.TotalGross = payroll.FixedSalary + payroll.BaseSalary + payroll.NormalWage + payroll.OvertimeWage
            + payroll.PieceworkWage + payroll.MealSubsidy + payroll.NightSubsidy + payroll.PerformanceBonus + payroll.OtherAllowance;
        var totalDeduction = payroll.SocialSecurityEmployee + payroll.ProvidentFundEmployee + payroll.IncomeTax + payroll.OtherDeduction;
        payroll.NetSalary = payroll.TotalGross - totalDeduction;
        payroll.TotalCompanyCost = payroll.TotalGross + payroll.SocialSecurityCompany + payroll.ProvidentFundCompany;

        payroll.Details = BuildDetails(payroll, normalHours, pieceworkQty, mealDays, nightDays);
    }

    private async Task<decimal> CalculateOvertimeWageAsync(Guid employeeTypeId, SalaryRule salaryRule, IEnumerable<Attendance> attendances)
    {
        var total = 0m;
        foreach (var attendance in attendances.Where(x => x.OvertimeHours > 0))
        {
            var multiplier = await ResolveOvertimeMultiplierAsync(employeeTypeId, attendance, salaryRule);
            var hourlyRate = salaryRule.HourlyRate
                ?? (salaryRule.FixedSalary.HasValue ? salaryRule.FixedSalary.Value / 21.75m / 8m : 0m);
            total += attendance.OvertimeHours * hourlyRate * multiplier;
        }

        return total;
    }

    private async Task<decimal> ResolveOvertimeMultiplierAsync(Guid employeeTypeId, Attendance attendance, SalaryRule salaryRule)
    {
        var holidayRule = await _holidayRuleRepository.GetByDateAsync(attendance.WorkDate.Date);
        if (holidayRule?.OvertimeMultiplier is > 0)
        {
            return holidayRule.OvertimeMultiplier.Value;
        }

        var overtimeType = !string.IsNullOrWhiteSpace(attendance.OvertimeType)
            ? attendance.OvertimeType!
            : holidayRule?.HolidayType ?? "Workday";

        var rateConfig = await _overtimeRateConfigRepository.GetActiveConfigAsync(employeeTypeId, overtimeType, attendance.WorkDate.Date);
        if (rateConfig != null)
        {
            return rateConfig.Multiplier;
        }

        return salaryRule.DefaultOvertimeMultiplier;
    }

    private static void ApplyAdjustments(Payroll payroll, IEnumerable<SalaryAdjustment> adjustments)
    {
        foreach (var adjustment in adjustments)
        {
            var type = adjustment.AdjustmentType?.Trim() ?? string.Empty;
            switch (type)
            {
                case "SocialSecurityPersonal":
                    payroll.SocialSecurityEmployee += adjustment.Amount;
                    break;
                case "SocialSecurityCompany":
                    payroll.SocialSecurityCompany += adjustment.Amount;
                    break;
                case "HousingFundPersonal":
                    payroll.ProvidentFundEmployee += adjustment.Amount;
                    break;
                case "HousingFundCompany":
                    payroll.ProvidentFundCompany += adjustment.Amount;
                    break;
                case "IncomeTax":
                    payroll.IncomeTax += adjustment.Amount;
                    break;
                case "MealAllowance":
                    payroll.MealSubsidy += adjustment.Amount;
                    break;
                case "NightShiftAllowance":
                    payroll.NightSubsidy += adjustment.Amount;
                    break;
                case "PerformanceBonus":
                    payroll.PerformanceBonus += adjustment.Amount;
                    break;
                case "Deduction":
                case "DisciplinaryDeduction":
                    payroll.OtherDeduction += Math.Abs(adjustment.Amount);
                    break;
                default:
                    if (adjustment.Amount >= 0)
                    {
                        payroll.OtherAllowance += adjustment.Amount;
                    }
                    else
                    {
                        payroll.OtherDeduction += Math.Abs(adjustment.Amount);
                    }
                    break;
            }
        }
    }

    private async Task<decimal> CalculateIncomeTaxAsync(int ruleYear, decimal taxableBase)
    {
        if (taxableBase <= 0)
        {
            return 0;
        }

        var rule = await _incomeTaxRuleRepository.GetMatchedRuleAsync(ruleYear, taxableBase);
        if (rule == null)
        {
            return 0;
        }

        var taxableAmount = taxableBase - rule.ThresholdAmount;
        if (taxableAmount <= 0)
        {
            return 0;
        }

        var tax = taxableAmount * rule.TaxRate - rule.QuickDeduction;
        return tax > 0 ? tax : 0;
    }

    private static List<PayrollDetail> BuildDetails(Payroll payroll, decimal normalHours, decimal pieceworkQty, int mealDays, int nightDays)
    {
        var details = new List<PayrollDetail>();
        AddDetail(details, payroll.Id, "fixed_salary", "固定工资", "Earning", payroll.FixedSalary, null, null, 10);
        AddDetail(details, payroll.Id, "base_salary", "底薪", "Earning", payroll.BaseSalary, null, null, 20);
        AddDetail(details, payroll.Id, "normal_wage", "正常工时工资", "Earning", payroll.NormalWage, normalHours, null, 30);
        AddDetail(details, payroll.Id, "overtime_wage", "加班工资", "Earning", payroll.OvertimeWage, null, null, 40);
        AddDetail(details, payroll.Id, "piecework_wage", "计件工资", "Earning", payroll.PieceworkWage, pieceworkQty, null, 50);
        AddDetail(details, payroll.Id, "meal_subsidy", "饭补", "Allowance", payroll.MealSubsidy, mealDays, null, 60);
        AddDetail(details, payroll.Id, "night_subsidy", "夜补", "Allowance", payroll.NightSubsidy, nightDays, null, 70);
        AddDetail(details, payroll.Id, "performance_bonus", "绩效奖金", "Allowance", payroll.PerformanceBonus, null, null, 80);
        AddDetail(details, payroll.Id, "other_allowance", "其他补贴", "Allowance", payroll.OtherAllowance, null, null, 90);
        AddDetail(details, payroll.Id, "social_security_employee", "社保个人", "Deduction", -payroll.SocialSecurityEmployee, null, null, 100);
        AddDetail(details, payroll.Id, "provident_fund_employee", "公积金个人", "Deduction", -payroll.ProvidentFundEmployee, null, null, 110);
        AddDetail(details, payroll.Id, "income_tax", "个税", "Deduction", -payroll.IncomeTax, null, null, 120);
        AddDetail(details, payroll.Id, "other_deduction", "其他扣款", "Deduction", -payroll.OtherDeduction, null, null, 130);
        return details;
    }

    private static void AddDetail(
        ICollection<PayrollDetail> details,
        Guid payrollId,
        string componentCode,
        string componentName,
        string componentCategory,
        decimal amount,
        decimal? quantity,
        decimal? unitPrice,
        int sortOrder)
    {
        if (amount == 0)
        {
            return;
        }

        details.Add(new PayrollDetail
        {
            PayrollId = payrollId,
            ComponentCode = componentCode,
            ComponentName = componentName,
            ComponentCategory = componentCategory,
            Amount = amount,
            Quantity = quantity,
            UnitPrice = unitPrice,
            SortOrder = sortOrder,
            SourceType = "System"
        });
    }

    private static bool IsProfileActive(EmployeePayrollProfile profile)
    {
        return string.Equals(profile.PayrollStatus, "Active", StringComparison.OrdinalIgnoreCase);
    }

    private static void ValidateYearMonth(string yearMonth)
    {
        if (string.IsNullOrWhiteSpace(yearMonth) || !System.Text.RegularExpressions.Regex.IsMatch(yearMonth, @"^\d{4}-\d{2}$"))
        {
            throw new InvalidOperationException("年月格式应为 YYYY-MM");
        }
    }

    private static (int Year, int Month) ParseYearMonth(string yearMonth)
    {
        var parts = yearMonth.Split('-');
        return (int.Parse(parts[0]), int.Parse(parts[1]));
    }

    private static string NormalizeSalaryMode(string salaryMode)
    {
        return salaryMode.Replace("+", string.Empty).Replace("-", string.Empty).Replace(" ", string.Empty);
    }

    private static string BuildRunNo(string runType)
    {
        var prefix = string.Equals(runType, "Trial", StringComparison.OrdinalIgnoreCase) ? "TRY" : "PAY";
        return $"{prefix}{DateTime.UtcNow:yyyyMMddHHmmssfff}";
    }

    private static void ValidateRollbackStatus(string status)
    {
        if (string.Equals(status, "RolledBack", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("当前薪资批次已回滚");
        }

        if (string.Equals(status, "Paid", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("已发放批次不能直接回滚");
        }

        if (!string.Equals(status, "Calculated", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(status, "Approved", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(status, "ApprovalRejected", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("当前薪资批次状态不允许回滚");
        }
    }

    private static void ValidateApprovalProcessInstance(PayrollRun run, string processInstanceId)
    {
        if (string.IsNullOrWhiteSpace(processInstanceId))
        {
            throw new InvalidOperationException("流程实例 ID 不能为空");
        }

        if (string.IsNullOrWhiteSpace(run.ApprovalProcessInstanceId))
        {
            throw new InvalidOperationException("当前薪资批次未绑定审批流程实例");
        }

        if (!string.Equals(run.ApprovalProcessInstanceId, processInstanceId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("审批流程实例与薪资批次不匹配");
        }
    }

    private static string ResolveWorkflowApprovedBy(string? operatorId, string? operatorName)
    {
        if (!string.IsNullOrWhiteSpace(operatorId))
        {
            return operatorId.Trim();
        }

        if (!string.IsNullOrWhiteSpace(operatorName))
        {
            return operatorName.Trim();
        }

        return "ProcessCenter";
    }

    private static string? ResolveWorkflowActor(string? operatorId, string? operatorName)
    {
        var normalizedOperatorId = string.IsNullOrWhiteSpace(operatorId) ? null : operatorId.Trim();
        var normalizedOperatorName = string.IsNullOrWhiteSpace(operatorName) ? null : operatorName.Trim();

        return (normalizedOperatorId, normalizedOperatorName) switch
        {
            ({ Length: > 0 } id, { Length: > 0 } name) => $"{name}/{id}",
            ({ Length: > 0 } id, _) => id,
            (_, { Length: > 0 } name) => name,
            _ => null
        };
    }

    private static string? AppendRemark(string? existingRemark, string action, string? remark, string? actionBy = null)
    {
        if (string.IsNullOrWhiteSpace(remark) && string.IsNullOrWhiteSpace(actionBy))
        {
            return existingRemark;
        }

        var actorText = string.IsNullOrWhiteSpace(actionBy) ? string.Empty : $" by {actionBy}";
        var content = string.IsNullOrWhiteSpace(remark) ? string.Empty : $" {remark.Trim()}";
        var newLine = $"[{action}{actorText}]{content}".TrimEnd();

        return string.IsNullOrWhiteSpace(existingRemark)
            ? newLine
            : $"{existingRemark}\n{newLine}";
    }

    private static PayrollRunDto MapRunDto(PayrollRun run, IEnumerable<Payroll> payrolls)
    {
        var payrollList = payrolls.Select(MapPayrollDto).ToList();
        return new PayrollRunDto
        {
            Id = run.Id,
            RunNo = run.RunNo,
            YearMonth = run.YearMonth,
            RunType = run.RunType,
            Status = run.Status,
            PayrollCount = payrollList.Count,
            TotalGross = payrollList.Sum(x => x.TotalGross),
            TotalNetSalary = payrollList.Sum(x => x.NetSalary),
            ApprovalProcessCode = run.ApprovalProcessCode,
            ApprovalProcessInstanceId = run.ApprovalProcessInstanceId,
            ApprovalRequestId = run.ApprovalRequestId,
            ApprovalSubmittedAt = run.ApprovalSubmittedAt,
            ApprovedBy = run.ApprovedBy,
            ApprovedAt = run.ApprovedAt,
            Remark = run.Remark,
            StartedAt = run.StartedAt,
            FinishedAt = run.FinishedAt,
            Payrolls = payrollList
        };
    }

    private static PayrollEmployeeResultDto MapPayrollDto(Payroll payroll)
    {
        return new PayrollEmployeeResultDto
        {
            PayrollId = payroll.Id,
            EmployeeId = payroll.EmployeeId,
            EmployeeNo = payroll.EmployeeNoSnapshot,
            EmployeeName = payroll.EmployeeNameSnapshot,
            EmployeeTypeId = payroll.EmployeeTypeId,
            EmployeeTypeName = payroll.EmployeeType?.TypeName ?? string.Empty,
            SalaryMode = payroll.SalaryModeSnapshot,
            Status = payroll.Status,
            NormalWage = payroll.NormalWage,
            OvertimeWage = payroll.OvertimeWage,
            PieceworkWage = payroll.PieceworkWage,
            FixedSalary = payroll.FixedSalary,
            BaseSalary = payroll.BaseSalary,
            MealSubsidy = payroll.MealSubsidy,
            NightSubsidy = payroll.NightSubsidy,
            PerformanceBonus = payroll.PerformanceBonus,
            OtherAllowance = payroll.OtherAllowance,
            SocialSecurityEmployee = payroll.SocialSecurityEmployee,
            SocialSecurityCompany = payroll.SocialSecurityCompany,
            ProvidentFundEmployee = payroll.ProvidentFundEmployee,
            ProvidentFundCompany = payroll.ProvidentFundCompany,
            IncomeTax = payroll.IncomeTax,
            OtherDeduction = payroll.OtherDeduction,
            TotalGross = payroll.TotalGross,
            NetSalary = payroll.NetSalary,
            TotalCompanyCost = payroll.TotalCompanyCost,
            PaidAt = payroll.PaidAt,
            Details = payroll.Details
                .OrderBy(x => x.SortOrder)
                .Select(x => new PayrollDetailDto
                {
                    Id = x.Id,
                    ComponentCode = x.ComponentCode,
                    ComponentName = x.ComponentName,
                    ComponentCategory = x.ComponentCategory,
                    Amount = x.Amount,
                    Quantity = x.Quantity,
                    UnitPrice = x.UnitPrice,
                    SortOrder = x.SortOrder,
                    SourceType = x.SourceType,
                    SourceId = x.SourceId,
                    Remark = x.Remark
                })
                .ToList()
        };
    }

    private sealed class PayrollExecutionContext
    {
        public string YearMonth { get; init; } = string.Empty;
        public int Year { get; init; }
        public int Month { get; init; }
        public DateTime CalculationDate { get; init; }
        public List<EmployeePayrollProfile> Profiles { get; init; } = [];
        public DateTime StartedAt { get; init; }
    }
}
