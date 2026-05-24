using HRMS.Domain.Entities;
using HRMS.Domain.Interfaces;
using HRMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Infrastructure.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly HrmsDbContext _context;

    public CustomerRepository(HrmsDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Customer>> GetAllAsync()
    {
        return await _context.Customers
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public Task<Customer?> GetByIdAsync(Guid id)
    {
        return _context.Customers.FirstOrDefaultAsync(x => x.Id == id);
    }

    public Task<Customer?> GetByCodeAsync(string code)
    {
        return _context.Customers.FirstOrDefaultAsync(x => x.Code == code);
    }

    public async Task<Customer> CreateAsync(Customer customer)
    {
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();
        return customer;
    }

    public async Task UpdateAsync(Customer customer)
    {
        customer.UpdatedAt = DateTime.UtcNow;
        _context.Customers.Update(customer);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Customer customer)
    {
        _context.Customers.Remove(customer);
        await _context.SaveChangesAsync();
    }
}
