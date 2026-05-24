using HRMS.Domain.Entities;
using HRMS.Domain.Interfaces;
using HRMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Infrastructure.Repositories;

public class EmployeeRepository : IEmployeeRepository
{
    private readonly HrmsDbContext _context;

    public EmployeeRepository(HrmsDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Employee>> GetAllAsync()
    {
        return await _context.Employees
            .Include(e => e.OrgUnit)
            .Include(e => e.ThirdPartyCompany)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();
    }

    public async Task<Employee?> GetByIdAsync(Guid id)
    {
        return await _context.Employees.Include(e => e.OrgUnit).Include(e => e.ThirdPartyCompany).FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<Employee?> GetByEmployeeNoAsync(string employeeNo)
    {
        return await _context.Employees.FirstOrDefaultAsync(e => e.EmployeeNo == employeeNo);
    }

    public async Task<IEnumerable<Employee>> GetByOrgUnitIdAsync(Guid orgUnitId)
    {
        return await _context.Employees.Where(e => e.OrgUnitId == orgUnitId && e.IsActive).ToListAsync();
    }

    public async Task<IEnumerable<Employee>> GetByThirdPartyCompanyAsync(Guid companyId)
    {
        return await _context.Employees.Where(e => e.ThirdPartyCompanyId == companyId && e.IsActive).ToListAsync();
    }

    public async Task<Employee> CreateAsync(Employee employee)
    {
        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();
        return employee;
    }

    public async Task UpdateAsync(Employee employee)
    {
        employee.UpdatedAt = DateTime.UtcNow;
        _context.Employees.Update(employee);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var employee = await _context.Employees.FindAsync(id);
        if (employee != null)
        {
            employee.IsActive = false;
            employee.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}
