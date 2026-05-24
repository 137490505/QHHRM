using HRMS.Domain.Entities;

namespace HRMS.Domain.Interfaces;

public interface IEmployeeRepository
{
    Task<IEnumerable<Employee>> GetAllAsync();
    Task<Employee?> GetByIdAsync(Guid id);
    Task<Employee?> GetByEmployeeNoAsync(string employeeNo);
    Task<IEnumerable<Employee>> GetByOrgUnitIdAsync(Guid orgUnitId);
    Task<IEnumerable<Employee>> GetByThirdPartyCompanyAsync(Guid companyId);
    Task<Employee> CreateAsync(Employee employee);
    Task UpdateAsync(Employee employee);
    Task DeleteAsync(Guid id);
}
