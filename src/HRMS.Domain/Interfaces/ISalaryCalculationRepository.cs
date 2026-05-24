using HRMS.Domain.Entities;

namespace HRMS.Domain.Interfaces;

public interface ISalaryCalculationRepository
{
    Task<IEnumerable<SalaryCalculation>> GetAllAsync();
    Task<SalaryCalculation?> GetByIdAsync(Guid id);
    Task<SalaryCalculation?> GetByEmployeeAndMonthAsync(Guid employeeId, int year, int month);
    Task<IEnumerable<SalaryCalculation>> GetByMonthAsync(int year, int month);
    Task<SalaryCalculation> CreateAsync(SalaryCalculation salaryCalculation);
    Task UpdateAsync(SalaryCalculation salaryCalculation);
}
