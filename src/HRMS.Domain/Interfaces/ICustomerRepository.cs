using HRMS.Domain.Entities;

namespace HRMS.Domain.Interfaces;

public interface ICustomerRepository
{
    Task<IEnumerable<Customer>> GetAllAsync();
    Task<Customer?> GetByIdAsync(Guid id);
    Task<Customer?> GetByCodeAsync(string code);
    Task<Customer> CreateAsync(Customer customer);
    Task UpdateAsync(Customer customer);
    Task DeleteAsync(Customer customer);
}
