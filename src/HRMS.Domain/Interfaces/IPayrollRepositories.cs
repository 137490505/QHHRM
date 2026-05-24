using HRMS.Domain.Entities;

namespace HRMS.Domain.Interfaces;

public interface IEmployeeTypeRepository
{
    Task<IEnumerable<PayrollEmployeeType>> GetAllAsync();
    Task<PayrollEmployeeType?> GetByIdAsync(Guid id);
    Task<PayrollEmployeeType?> GetByCodeAsync(string typeCode);
    Task<PayrollEmployeeType> CreateAsync(PayrollEmployeeType entity);
    Task UpdateAsync(PayrollEmployeeType entity);
}

public interface IEmployeePayrollProfileRepository
{
    Task<IEnumerable<EmployeePayrollProfile>> GetAllAsync();
    Task<EmployeePayrollProfile?> GetByIdAsync(Guid id);
    Task<EmployeePayrollProfile?> GetByEmployeeIdAsync(Guid employeeId);
    Task<EmployeePayrollProfile> CreateAsync(EmployeePayrollProfile entity);
    Task UpdateAsync(EmployeePayrollProfile entity);
}

public interface ISalaryRuleRepository
{
    Task<IEnumerable<SalaryRule>> GetAllAsync();
    Task<SalaryRule?> GetByIdAsync(Guid id);
    Task<IEnumerable<SalaryRule>> GetByEmployeeTypeAsync(Guid employeeTypeId);
    Task<SalaryRule?> GetActiveRuleAsync(Guid employeeTypeId, DateTime effectiveDate);
    Task<SalaryRule> CreateAsync(SalaryRule entity);
    Task UpdateAsync(SalaryRule entity);
}

public interface IOvertimeRateConfigRepository
{
    Task<IEnumerable<OvertimeRateConfig>> GetAllAsync();
    Task<OvertimeRateConfig?> GetByIdAsync(Guid id);
    Task<IEnumerable<OvertimeRateConfig>> GetByEmployeeTypeAsync(Guid employeeTypeId);
    Task<OvertimeRateConfig?> GetActiveConfigAsync(Guid employeeTypeId, string holidayType, DateTime effectiveDate);
    Task<OvertimeRateConfig?> GetByCodeAsync(string configCode);
    Task<OvertimeRateConfig> CreateAsync(OvertimeRateConfig entity);
    Task UpdateAsync(OvertimeRateConfig entity);
}

public interface IHolidayRuleRepository
{
    Task<IEnumerable<HolidayRule>> GetAllAsync();
    Task<HolidayRule?> GetByIdAsync(Guid id);
    Task<IEnumerable<HolidayRule>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<HolidayRule?> GetByDateAsync(DateTime holidayDate);
    Task<HolidayRule?> GetByCodeAsync(string holidayCode);
    Task<HolidayRule?> GetByDateAndTypeAsync(DateTime holidayDate, string holidayType);
    Task<HolidayRule> CreateAsync(HolidayRule entity);
    Task UpdateAsync(HolidayRule entity);
}

public interface IAttendanceRepository
{
    Task<Attendance?> GetByIdAsync(Guid id);
    Task<IEnumerable<Attendance>> GetByEmployeeAndMonthAsync(Guid employeeId, int year, int month);
    Task<IEnumerable<Attendance>> GetByMonthAsync(int year, int month);
    Task<Attendance?> GetByEmployeeAndDateAsync(Guid employeeId, DateTime workDate);
    Task<Attendance> CreateAsync(Attendance entity);
    Task CreateRangeAsync(IEnumerable<Attendance> entities);
    Task UpdateAsync(Attendance entity);
}

public interface ISalaryAdjustmentRepository
{
    Task<SalaryAdjustment?> GetByIdAsync(Guid id);
    Task<IEnumerable<SalaryAdjustment>> GetByEmployeeAndMonthAsync(Guid employeeId, string yearMonth);
    Task<IEnumerable<SalaryAdjustment>> GetByMonthAsync(string yearMonth);
    Task<SalaryAdjustment> CreateAsync(SalaryAdjustment entity);
    Task CreateRangeAsync(IEnumerable<SalaryAdjustment> entities);
    Task UpdateAsync(SalaryAdjustment entity);
}

public interface IIncomeTaxRuleRepository
{
    Task<IEnumerable<IncomeTaxRule>> GetAllAsync();
    Task<IncomeTaxRule?> GetByIdAsync(Guid id);
    Task<IEnumerable<IncomeTaxRule>> GetByYearAsync(int ruleYear);
    Task<IncomeTaxRule?> GetByYearAndLevelAsync(int ruleYear, int levelNo);
    Task<IncomeTaxRule?> GetMatchedRuleAsync(int ruleYear, decimal taxableAmount);
    Task<IncomeTaxRule> CreateAsync(IncomeTaxRule entity);
    Task UpdateAsync(IncomeTaxRule entity);
}

public interface IPayrollRunRepository
{
    Task<IEnumerable<PayrollRun>> GetByMonthAsync(string yearMonth);
    Task<PayrollRun?> GetByIdAsync(Guid id);
    Task<PayrollRun?> GetByRunNoAsync(string runNo);
    Task<PayrollRun> CreateAsync(PayrollRun entity);
    Task UpdateAsync(PayrollRun entity);
}

public interface IPayrollRepository
{
    Task<IEnumerable<Payroll>> GetByMonthAsync(string yearMonth);
    Task<IEnumerable<Payroll>> GetByEmployeeAndMonthAsync(Guid employeeId, string yearMonth);
    Task<IEnumerable<Payroll>> GetByRunIdAsync(Guid payrollRunId);
    Task<Payroll?> GetByIdAsync(Guid id);
    Task CreateRangeAsync(IEnumerable<Payroll> entities);
    Task UpdateAsync(Payroll entity);
}
