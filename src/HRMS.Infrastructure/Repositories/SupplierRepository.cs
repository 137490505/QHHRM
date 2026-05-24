using HRMS.Domain.Entities;
using HRMS.Domain.Interfaces;
using HRMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Infrastructure.Repositories;

public class SupplierRepository : ISupplierRepository
{
    private readonly HrmsDbContext _context;

    public SupplierRepository(HrmsDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Supplier>> GetAllAsync()
    {
        return await _context.Suppliers
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public Task<Supplier?> GetByIdAsync(Guid id)
    {
        return _context.Suppliers.FirstOrDefaultAsync(x => x.Id == id);
    }

    public Task<Supplier?> GetByCodeAsync(string code)
    {
        return _context.Suppliers.FirstOrDefaultAsync(x => x.Code == code);
    }

    public async Task<Supplier> CreateAsync(Supplier supplier)
    {
        _context.Suppliers.Add(supplier);
        await _context.SaveChangesAsync();
        return supplier;
    }

    public async Task UpdateAsync(Supplier supplier)
    {
        supplier.UpdatedAt = DateTime.UtcNow;
        _context.Suppliers.Update(supplier);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Supplier supplier)
    {
        _context.Suppliers.Remove(supplier);
        await _context.SaveChangesAsync();
    }
}
