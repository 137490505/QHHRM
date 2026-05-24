using HRMS.Domain.Entities;
using HRMS.Domain.Interfaces;
using HRMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Infrastructure.Repositories;

public class EmployeeTypeRepository : IEmployeeTypeRepository
{
    private readonly HrmsDbContext _context;

    public EmployeeTypeRepository(HrmsDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<PayrollEmployeeType>> GetAllAsync()
    {
        return await _context.EmployeeTypes
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.TypeName)
            .ToListAsync();
    }

    public async Task<PayrollEmployeeType?> GetByIdAsync(Guid id)
    {
        return await _context.EmployeeTypes.FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<PayrollEmployeeType?> GetByCodeAsync(string typeCode)
    {
        return await _context.EmployeeTypes.FirstOrDefaultAsync(x => x.TypeCode == typeCode);
    }

    public async Task<PayrollEmployeeType> CreateAsync(PayrollEmployeeType entity)
    {
        _context.EmployeeTypes.Add(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task UpdateAsync(PayrollEmployeeType entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _context.EmployeeTypes.Update(entity);
        await _context.SaveChangesAsync();
    }
}

public class EmployeePayrollProfileRepository : IEmployeePayrollProfileRepository
{
    private readonly HrmsDbContext _context;

    public EmployeePayrollProfileRepository(HrmsDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<EmployeePayrollProfile>> GetAllAsync()
    {
        return await _context.EmployeePayrollProfiles
            .Include(x => x.Employee)
            .Include(x => x.EmployeeType)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<EmployeePayrollProfile?> GetByIdAsync(Guid id)
    {
        return await _context.EmployeePayrollProfiles
            .Include(x => x.Employee)
            .Include(x => x.EmployeeType)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<EmployeePayrollProfile?> GetByEmployeeIdAsync(Guid employeeId)
    {
        return await _context.EmployeePayrollProfiles
            .Include(x => x.Employee)
            .Include(x => x.EmployeeType)
            .FirstOrDefaultAsync(x => x.EmployeeId == employeeId);
    }

    public async Task<EmployeePayrollProfile> CreateAsync(EmployeePayrollProfile entity)
    {
        _context.EmployeePayrollProfiles.Add(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task UpdateAsync(EmployeePayrollProfile entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _context.EmployeePayrollProfiles.Update(entity);
        await _context.SaveChangesAsync();
    }
}

public class SalaryRuleRepository : ISalaryRuleRepository
{
    private readonly HrmsDbContext _context;

    public SalaryRuleRepository(HrmsDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<SalaryRule>> GetAllAsync()
    {
        return await _context.SalaryRules
            .Include(x => x.EmployeeType)
            .OrderByDescending(x => x.EffectiveStart)
            .ToListAsync();
    }

    public async Task<SalaryRule?> GetByIdAsync(Guid id)
    {
        return await _context.SalaryRules
            .Include(x => x.EmployeeType)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<IEnumerable<SalaryRule>> GetByEmployeeTypeAsync(Guid employeeTypeId)
    {
        return await _context.SalaryRules
            .Where(x => x.EmployeeTypeId == employeeTypeId)
            .Include(x => x.EmployeeType)
            .OrderByDescending(x => x.EffectiveStart)
            .ToListAsync();
    }

    public async Task<SalaryRule?> GetActiveRuleAsync(Guid employeeTypeId, DateTime effectiveDate)
    {
        return await _context.SalaryRules
            .Include(x => x.EmployeeType)
            .Where(x => x.EmployeeTypeId == employeeTypeId
                && x.IsActive
                && x.EffectiveStart <= effectiveDate
                && (x.EffectiveEnd == null || x.EffectiveEnd >= effectiveDate))
            .OrderByDescending(x => x.EffectiveStart)
            .FirstOrDefaultAsync();
    }

    public async Task<SalaryRule> CreateAsync(SalaryRule entity)
    {
        _context.SalaryRules.Add(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task UpdateAsync(SalaryRule entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _context.SalaryRules.Update(entity);
        await _context.SaveChangesAsync();
    }
}

public class OvertimeRateConfigRepository : IOvertimeRateConfigRepository
{
    private readonly HrmsDbContext _context;

    public OvertimeRateConfigRepository(HrmsDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<OvertimeRateConfig>> GetAllAsync()
    {
        return await _context.OvertimeRateConfigs
            .Include(x => x.EmployeeType)
            .OrderByDescending(x => x.EffectiveStart)
            .ThenBy(x => x.ConfigCode)
            .ToListAsync();
    }

    public async Task<OvertimeRateConfig?> GetByIdAsync(Guid id)
    {
        return await _context.OvertimeRateConfigs
            .Include(x => x.EmployeeType)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<IEnumerable<OvertimeRateConfig>> GetByEmployeeTypeAsync(Guid employeeTypeId)
    {
        return await _context.OvertimeRateConfigs
            .Where(x => x.EmployeeTypeId == employeeTypeId)
            .Include(x => x.EmployeeType)
            .OrderByDescending(x => x.EffectiveStart)
            .ToListAsync();
    }

    public async Task<OvertimeRateConfig?> GetActiveConfigAsync(Guid employeeTypeId, string holidayType, DateTime effectiveDate)
    {
        return await _context.OvertimeRateConfigs
            .Include(x => x.EmployeeType)
            .Where(x => x.EmployeeTypeId == employeeTypeId
                && x.HolidayType == holidayType
                && x.IsActive
                && x.EffectiveStart <= effectiveDate
                && (x.EffectiveEnd == null || x.EffectiveEnd >= effectiveDate))
            .OrderByDescending(x => x.EffectiveStart)
            .FirstOrDefaultAsync();
    }

    public async Task<OvertimeRateConfig?> GetByCodeAsync(string configCode)
    {
        return await _context.OvertimeRateConfigs
            .Include(x => x.EmployeeType)
            .FirstOrDefaultAsync(x => x.ConfigCode == configCode);
    }

    public async Task<OvertimeRateConfig> CreateAsync(OvertimeRateConfig entity)
    {
        _context.OvertimeRateConfigs.Add(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task UpdateAsync(OvertimeRateConfig entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _context.OvertimeRateConfigs.Update(entity);
        await _context.SaveChangesAsync();
    }
}

public class HolidayRuleRepository : IHolidayRuleRepository
{
    private readonly HrmsDbContext _context;

    public HolidayRuleRepository(HrmsDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<HolidayRule>> GetAllAsync()
    {
        return await _context.HolidayRules
            .OrderByDescending(x => x.HolidayDate)
            .ThenBy(x => x.HolidayType)
            .ToListAsync();
    }

    public async Task<HolidayRule?> GetByIdAsync(Guid id)
    {
        return await _context.HolidayRules.FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<IEnumerable<HolidayRule>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.HolidayRules
            .Where(x => x.HolidayDate >= startDate && x.HolidayDate <= endDate)
            .OrderBy(x => x.HolidayDate)
            .ToListAsync();
    }

    public async Task<HolidayRule?> GetByDateAsync(DateTime holidayDate)
    {
        return await _context.HolidayRules
            .Where(x => x.HolidayDate == holidayDate && x.IsActive)
            .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<HolidayRule?> GetByCodeAsync(string holidayCode)
    {
        return await _context.HolidayRules.FirstOrDefaultAsync(x => x.HolidayCode == holidayCode);
    }

    public async Task<HolidayRule?> GetByDateAndTypeAsync(DateTime holidayDate, string holidayType)
    {
        return await _context.HolidayRules
            .FirstOrDefaultAsync(x => x.HolidayDate == holidayDate.Date && x.HolidayType == holidayType);
    }

    public async Task<HolidayRule> CreateAsync(HolidayRule entity)
    {
        _context.HolidayRules.Add(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task UpdateAsync(HolidayRule entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _context.HolidayRules.Update(entity);
        await _context.SaveChangesAsync();
    }
}

public class AttendanceRepository : IAttendanceRepository
{
    private readonly HrmsDbContext _context;

    public AttendanceRepository(HrmsDbContext context)
    {
        _context = context;
    }

    public async Task<Attendance?> GetByIdAsync(Guid id)
    {
        return await _context.Attendances
            .Include(x => x.Employee)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<IEnumerable<Attendance>> GetByEmployeeAndMonthAsync(Guid employeeId, int year, int month)
    {
        return await _context.Attendances
            .Where(x => x.EmployeeId == employeeId && x.WorkDate.Year == year && x.WorkDate.Month == month)
            .Include(x => x.Employee)
            .OrderBy(x => x.WorkDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Attendance>> GetByMonthAsync(int year, int month)
    {
        return await _context.Attendances
            .Where(x => x.WorkDate.Year == year && x.WorkDate.Month == month)
            .Include(x => x.Employee)
            .OrderBy(x => x.WorkDate)
            .ToListAsync();
    }

    public async Task<Attendance?> GetByEmployeeAndDateAsync(Guid employeeId, DateTime workDate)
    {
        return await _context.Attendances
            .Include(x => x.Employee)
            .FirstOrDefaultAsync(x => x.EmployeeId == employeeId && x.WorkDate == workDate.Date);
    }

    public async Task<Attendance> CreateAsync(Attendance entity)
    {
        _context.Attendances.Add(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task CreateRangeAsync(IEnumerable<Attendance> entities)
    {
        _context.Attendances.AddRange(entities);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Attendance entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _context.Attendances.Update(entity);
        await _context.SaveChangesAsync();
    }
}

public class SalaryAdjustmentRepository : ISalaryAdjustmentRepository
{
    private readonly HrmsDbContext _context;

    public SalaryAdjustmentRepository(HrmsDbContext context)
    {
        _context = context;
    }

    public async Task<SalaryAdjustment?> GetByIdAsync(Guid id)
    {
        return await _context.SalaryAdjustments
            .Include(x => x.Employee)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<IEnumerable<SalaryAdjustment>> GetByEmployeeAndMonthAsync(Guid employeeId, string yearMonth)
    {
        return await _context.SalaryAdjustments
            .Where(x => x.EmployeeId == employeeId && x.YearMonth == yearMonth)
            .Include(x => x.Employee)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<SalaryAdjustment>> GetByMonthAsync(string yearMonth)
    {
        return await _context.SalaryAdjustments
            .Where(x => x.YearMonth == yearMonth)
            .Include(x => x.Employee)
            .OrderBy(x => x.EmployeeId)
            .ThenBy(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<SalaryAdjustment> CreateAsync(SalaryAdjustment entity)
    {
        _context.SalaryAdjustments.Add(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task CreateRangeAsync(IEnumerable<SalaryAdjustment> entities)
    {
        _context.SalaryAdjustments.AddRange(entities);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(SalaryAdjustment entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _context.SalaryAdjustments.Update(entity);
        await _context.SaveChangesAsync();
    }
}

public class IncomeTaxRuleRepository : IIncomeTaxRuleRepository
{
    private readonly HrmsDbContext _context;

    public IncomeTaxRuleRepository(HrmsDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<IncomeTaxRule>> GetAllAsync()
    {
        return await _context.IncomeTaxRules
            .OrderByDescending(x => x.RuleYear)
            .ThenBy(x => x.LevelNo)
            .ToListAsync();
    }

    public async Task<IncomeTaxRule?> GetByIdAsync(Guid id)
    {
        return await _context.IncomeTaxRules.FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<IEnumerable<IncomeTaxRule>> GetByYearAsync(int ruleYear)
    {
        return await _context.IncomeTaxRules
            .Where(x => x.RuleYear == ruleYear && x.IsActive)
            .OrderBy(x => x.LevelNo)
            .ToListAsync();
    }

    public async Task<IncomeTaxRule?> GetByYearAndLevelAsync(int ruleYear, int levelNo)
    {
        return await _context.IncomeTaxRules
            .FirstOrDefaultAsync(x => x.RuleYear == ruleYear && x.LevelNo == levelNo);
    }

    public async Task<IncomeTaxRule?> GetMatchedRuleAsync(int ruleYear, decimal taxableAmount)
    {
        return await _context.IncomeTaxRules
            .Where(x => x.RuleYear == ruleYear
                && x.IsActive
                && x.MinTaxableAmount <= taxableAmount
                && (x.MaxTaxableAmount == null || x.MaxTaxableAmount >= taxableAmount))
            .OrderBy(x => x.LevelNo)
            .FirstOrDefaultAsync();
    }

    public async Task<IncomeTaxRule> CreateAsync(IncomeTaxRule entity)
    {
        _context.IncomeTaxRules.Add(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task UpdateAsync(IncomeTaxRule entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _context.IncomeTaxRules.Update(entity);
        await _context.SaveChangesAsync();
    }
}

public class PayrollRunRepository : IPayrollRunRepository
{
    private readonly HrmsDbContext _context;

    public PayrollRunRepository(HrmsDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<PayrollRun>> GetByMonthAsync(string yearMonth)
    {
        return await _context.PayrollRuns
            .Include(x => x.Payrolls)
            .Where(x => x.YearMonth == yearMonth)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<PayrollRun?> GetByIdAsync(Guid id)
    {
        return await _context.PayrollRuns
            .Include(x => x.Payrolls)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<PayrollRun?> GetByRunNoAsync(string runNo)
    {
        return await _context.PayrollRuns
            .Include(x => x.Payrolls)
            .FirstOrDefaultAsync(x => x.RunNo == runNo);
    }

    public async Task<PayrollRun> CreateAsync(PayrollRun entity)
    {
        _context.PayrollRuns.Add(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task UpdateAsync(PayrollRun entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _context.PayrollRuns.Update(entity);
        await _context.SaveChangesAsync();
    }
}

public class PayrollRepository : IPayrollRepository
{
    private readonly HrmsDbContext _context;

    public PayrollRepository(HrmsDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Payroll>> GetByMonthAsync(string yearMonth)
    {
        return await _context.Payrolls
            .Include(x => x.Employee)
            .Include(x => x.EmployeeType)
            .Include(x => x.PayrollRun)
            .Where(x => x.YearMonth == yearMonth)
            .OrderBy(x => x.EmployeeNameSnapshot)
            .ToListAsync();
    }

    public async Task<IEnumerable<Payroll>> GetByEmployeeAndMonthAsync(Guid employeeId, string yearMonth)
    {
        return await _context.Payrolls
            .Include(x => x.PayrollRun)
            .Include(x => x.Details)
            .Where(x => x.EmployeeId == employeeId && x.YearMonth == yearMonth)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Payroll>> GetByRunIdAsync(Guid payrollRunId)
    {
        return await _context.Payrolls
            .Include(x => x.Employee)
            .Include(x => x.EmployeeType)
            .Include(x => x.Details)
            .Where(x => x.PayrollRunId == payrollRunId)
            .OrderBy(x => x.EmployeeNameSnapshot)
            .ToListAsync();
    }

    public async Task<Payroll?> GetByIdAsync(Guid id)
    {
        return await _context.Payrolls
            .Include(x => x.Employee)
            .Include(x => x.EmployeeType)
            .Include(x => x.PayrollRun)
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task CreateRangeAsync(IEnumerable<Payroll> entities)
    {
        _context.Payrolls.AddRange(entities);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Payroll entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _context.Payrolls.Update(entity);
        await _context.SaveChangesAsync();
    }
}
