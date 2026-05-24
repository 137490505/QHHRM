using HRMS.Domain.Entities;
using HRMS.Domain.Interfaces;
using HRMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Infrastructure.Repositories;

public class SalaryCalculationRepository : ISalaryCalculationRepository
{
    private readonly HrmsDbContext _context;

    public SalaryCalculationRepository(HrmsDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<SalaryCalculation>> GetAllAsync()
    {
        return await _context.SalaryCalculations.Include(s => s.Employee).ToListAsync();
    }

    public async Task<SalaryCalculation?> GetByIdAsync(Guid id)
    {
        return await _context.SalaryCalculations.Include(s => s.Employee).FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<SalaryCalculation?> GetByEmployeeAndMonthAsync(Guid employeeId, int year, int month)
    {
        return await _context.SalaryCalculations
            .FirstOrDefaultAsync(s => s.EmployeeId == employeeId && s.Year == year && s.Month == month);
    }

    public async Task<IEnumerable<SalaryCalculation>> GetByMonthAsync(int year, int month)
    {
        return await _context.SalaryCalculations
            .Where(s => s.Year == year && s.Month == month)
            .Include(s => s.Employee)
            .ToListAsync();
    }

    public async Task<SalaryCalculation> CreateAsync(SalaryCalculation salaryCalculation)
    {
        _context.SalaryCalculations.Add(salaryCalculation);
        await _context.SaveChangesAsync();
        return salaryCalculation;
    }

    public async Task UpdateAsync(SalaryCalculation salaryCalculation)
    {
        _context.SalaryCalculations.Update(salaryCalculation);
        await _context.SaveChangesAsync();
    }
}
