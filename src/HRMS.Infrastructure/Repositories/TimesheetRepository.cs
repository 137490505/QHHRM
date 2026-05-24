using HRMS.Domain.Entities;
using HRMS.Domain.Interfaces;
using HRMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Infrastructure.Repositories;

public class TimesheetRepository : ITimesheetRepository
{
    private readonly HrmsDbContext _context;

    public TimesheetRepository(HrmsDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Timesheet>> GetAllAsync()
    {
        return await _context.Timesheets.Include(t => t.Employee).Include(t => t.ActualOrgUnit).ToListAsync();
    }

    public async Task<Timesheet?> GetByIdAsync(Guid id)
    {
        return await _context.Timesheets.Include(t => t.Employee).Include(t => t.ActualOrgUnit).FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<IEnumerable<Timesheet>> GetByEmployeeAndMonthAsync(Guid employeeId, int year, int month)
    {
        return await _context.Timesheets
            .Where(t => t.EmployeeId == employeeId && t.Date.Year == year && t.Date.Month == month)
            .Include(t => t.ActualOrgUnit)
            .ToListAsync();
    }

    public async Task<IEnumerable<Timesheet>> GetByOrgUnitAndDateRangeAsync(Guid orgUnitId, DateTime startDate, DateTime endDate)
    {
        return await _context.Timesheets
            .Where(t => t.ActualOrgUnitId == orgUnitId && t.Date >= startDate && t.Date <= endDate)
            .Include(t => t.Employee)
            .ToListAsync();
    }

    public async Task<Timesheet> CreateAsync(Timesheet timesheet)
    {
        _context.Timesheets.Add(timesheet);
        await _context.SaveChangesAsync();
        return timesheet;
    }

    public async Task CreateRangeAsync(IEnumerable<Timesheet> timesheets)
    {
        _context.Timesheets.AddRange(timesheets);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Timesheet timesheet)
    {
        timesheet.UpdatedAt = DateTime.UtcNow;
        _context.Timesheets.Update(timesheet);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var timesheet = await _context.Timesheets.FindAsync(id);
        if (timesheet != null)
        {
            _context.Timesheets.Remove(timesheet);
            await _context.SaveChangesAsync();
        }
    }
}
