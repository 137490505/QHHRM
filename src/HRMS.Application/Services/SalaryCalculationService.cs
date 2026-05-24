using HRMS.Application.DTOs;
using HRMS.Domain.Entities;
using HRMS.Domain.Enums;
using HRMS.Domain.Interfaces;

namespace HRMS.Application.Services;

public class SalaryCalculationService
{
    private readonly ISalaryCalculationRepository _salaryRepository;
    private readonly ITimesheetRepository _timesheetRepository;
    private readonly IEmployeeRepository _employeeRepository;

    public SalaryCalculationService(
        ISalaryCalculationRepository salaryRepository,
        ITimesheetRepository timesheetRepository,
        IEmployeeRepository employeeRepository)
    {
        _salaryRepository = salaryRepository;
        _timesheetRepository = timesheetRepository;
        _employeeRepository = employeeRepository;
    }

    public async Task<IEnumerable<SalaryCalculationDto>> GetAllAsync()
    {
        var calculations = await _salaryRepository.GetAllAsync();
        return calculations.Select(MapToDto);
    }

    public async Task<SalaryCalculationDto?> GetByIdAsync(Guid id)
    {
        var calculation = await _salaryRepository.GetByIdAsync(id);
        return calculation == null ? null : MapToDto(calculation);
    }

    public async Task<SalaryCalculationDto?> GetByEmployeeAndMonthAsync(Guid employeeId, int year, int month)
    {
        var calculation = await _salaryRepository.GetByEmployeeAndMonthAsync(employeeId, year, month);
        return calculation == null ? null : MapToDto(calculation);
    }

    public async Task<IEnumerable<SalaryCalculationDto>> CalculateAsync(CalculateSalaryDto dto)
    {
        var employees = dto.EmployeeIds?.Any() == true
            ? await GetEmployeesByIds(dto.EmployeeIds)
            : await _employeeRepository.GetAllAsync();

        if (dto.OrgUnitIds?.Any() == true)
        {
            employees = employees.Where(e => dto.OrgUnitIds.Contains(e.OrgUnitId));
        }

        var results = new List<SalaryCalculationDto>();
        var config = GetSalaryConfig();

        foreach (var employee in employees)
        {
            var timesheets = await _timesheetRepository.GetByEmployeeAndMonthAsync(employee.Id, dto.Year, dto.Month);
            var approvedTimesheets = timesheets.Where(t => t.ApprovalStatus == ApprovalStatus.Approved).ToList();

            if (!approvedTimesheets.Any()) continue;

            var calculation = CalculateEmployeeSalary(employee, approvedTimesheets, config);
            await _salaryRepository.CreateAsync(calculation);
            results.Add(MapToDto(calculation));
        }

        return results;
    }

    private async Task<IEnumerable<Employee>> GetEmployeesByIds(List<Guid> ids)
    {
        var allEmployees = await _employeeRepository.GetAllAsync();
        return allEmployees.Where(e => ids.Contains(e.Id));
    }

    private SalaryCalculation CalculateEmployeeSalary(Employee employee, List<Timesheet> timesheets, SalaryConfigDto config)
 {
        var totalRegularHours = timesheets.Sum(t => t.WorkingHours - t.OvertimeHours);
        var totalOvertimeHours = timesheets.Sum(t => t.OvertimeHours);
        var totalHours = timesheets.Sum(t => t.WorkingHours);

        decimal regularWages = 0;
        decimal overtimeWages = 0;

        switch (employee.SalaryMode)
        {
            case SalaryMode.Hourly:
                var hourlyRate = employee.HourlyRate ?? 0;
                regularWages = totalRegularHours * hourlyRate;
                overtimeWages = totalOvertimeHours * hourlyRate * config.WeekdayOvertimeMultiplier;
                break;

            case SalaryMode.Fixed:
                var monthlySalary = employee.MonthlySalary ?? 0;
                var hourlyBase = monthlySalary / 21.75m / config.StandardDailyHours;
                regularWages = totalRegularHours * hourlyBase;
                overtimeWages = totalOvertimeHours * hourlyBase * config.WeekdayOvertimeMultiplier;
                break;

            case SalaryMode.PieceRate:
            case SalaryMode.Mixed:
                var baseHourlyRate = employee.HourlyRate ?? 0;
                regularWages = totalHours * baseHourlyRate;
                break;
        }

        var mealAllowance = CalculateMealAllowance(totalHours, config.MealAllowanceTiers);

        var calculation = new SalaryCalculation
        {
            EmployeeId = employee.Id,
            Year = timesheets.First().Date.Year,
            Month = timesheets.First().Date.Month,
            RegularHours = totalRegularHours,
            OvertimeHours = totalOvertimeHours,
            RegularWages = regularWages,
            OvertimeWages = overtimeWages,
            MealAllowance = mealAllowance,
            GrossWages = regularWages + overtimeWages + mealAllowance
        };

        if (employee.EmployeeType == EmployeeType.Internal)
        {
            calculation.SocialSecurityPersonal = CalculateSocialSecurity(calculation.GrossWages);
            calculation.HousingFundPersonal = CalculateHousingFund(calculation.GrossWages);
        }

        calculation.TotalDeductions = calculation.SocialSecurityPersonal + calculation.HousingFundPersonal + calculation.IncomeTax + calculation.AbsenceDeduction + calculation.OtherDeductions;
        calculation.NetWages = calculation.GrossWages - calculation.TotalDeductions;

        return calculation;
    }

    private static decimal CalculateMealAllowance(decimal totalHours, List<MealAllowanceTierDto> tiers)
    {
        foreach (var tier in tiers.OrderByDescending(t => t.MinHours))
        {
            if (totalHours >= tier.MinHours && totalHours < tier.MaxHours)
            {
                return tier.Amount;
            }
        }
        return 0;
    }

    private static decimal CalculateSocialSecurity(decimal grossWages)
    {
        return grossWages * 0.105m;
    }

    private static decimal CalculateHousingFund(decimal grossWages)
    {
        return grossWages * 0.12m;
    }

    private static SalaryConfigDto GetSalaryConfig()
    {
        return new SalaryConfigDto
        {
            TrialDays = 3,
            TrialDailySalary = 80m,
            TrialNoSalaryIfInsufficient = true,
            TrialIgnoreOvertime = true,
            StandardDailyHours = 8.0m,
            WeekdayOvertimeMultiplier = 1.5m,
            WeekendOvertimeMultiplier = 2.0m,
            HolidayOvertimeMultiplier = 3.0m
        };
    }

    private static SalaryCalculationDto MapToDto(SalaryCalculation calculation)
    {
        return new SalaryCalculationDto
        {
            Id = calculation.Id,
            EmployeeId = calculation.EmployeeId,
            EmployeeName = calculation.Employee?.Name ?? string.Empty,
            EmployeeNo = calculation.Employee?.EmployeeNo ?? string.Empty,
            Year = calculation.Year,
            Month = calculation.Month,
            RegularHours = calculation.RegularHours,
            OvertimeHours = calculation.OvertimeHours,
            RegularWages = calculation.RegularWages,
            OvertimeWages = calculation.OvertimeWages,
            MealAllowance = calculation.MealAllowance,
            Benefits = calculation.Benefits,
            PerformanceBonus = calculation.PerformanceBonus,
            AbsenceDeduction = calculation.AbsenceDeduction,
            OtherDeductions = calculation.OtherDeductions,
            GrossWages = calculation.GrossWages,
            SocialSecurityPersonal = calculation.SocialSecurityPersonal,
            HousingFundPersonal = calculation.HousingFundPersonal,
            IncomeTax = calculation.IncomeTax,
            TotalDeductions = calculation.TotalDeductions,
            NetWages = calculation.NetWages,
            CalculatedAt = calculation.CalculatedAt
        };
    }
}
