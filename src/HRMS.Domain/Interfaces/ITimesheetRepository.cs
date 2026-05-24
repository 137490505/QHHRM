using HRMS.Domain.Entities;

namespace HRMS.Domain.Interfaces;

public interface ITimesheetRepository
{
    Task<IEnumerable<Timesheet>> GetAllAsync();
    Task<Timesheet?> GetByIdAsync(Guid id);
    Task<IEnumerable<Timesheet>> GetByEmployeeAndMonthAsync(Guid employeeId, int year, int month);
    Task<IEnumerable<Timesheet>> GetByOrgUnitAndDateRangeAsync(Guid orgUnitId, DateTime startDate, DateTime endDate);
    Task<Timesheet> CreateAsync(Timesheet timesheet);
    Task CreateRangeAsync(IEnumerable<Timesheet> timesheets);
    Task UpdateAsync(Timesheet timesheet);
    Task DeleteAsync(Guid id);
}
