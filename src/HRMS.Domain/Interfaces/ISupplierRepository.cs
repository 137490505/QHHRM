using HRMS.Domain.Entities;

namespace HRMS.Domain.Interfaces;

public interface ISupplierRepository
{
    Task<IEnumerable<Supplier>> GetAllAsync();
    Task<Supplier?> GetByIdAsync(Guid id);
    Task<Supplier?> GetByCodeAsync(string code);
    Task<Supplier> CreateAsync(Supplier supplier);
    Task UpdateAsync(Supplier supplier);
    Task DeleteAsync(Supplier supplier);
}
