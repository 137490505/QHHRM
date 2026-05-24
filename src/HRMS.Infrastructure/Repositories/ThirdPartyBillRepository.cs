using HRMS.Infrastructure.Data;
using HRMS.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Infrastructure.Repositories;

public class ThirdPartyBillRepository : HRMS.Domain.Interfaces.IThirdPartyBillRepository
{
    private readonly HrmsDbContext _context;

    public ThirdPartyBillRepository(HrmsDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<HRMS.Domain.Entities.ThirdPartyBill>> GetAllAsync()
    {
        return await _context.ThirdPartyBills.Include(b => b.Client).Include(b => b.Details).ToListAsync();
    }

    public async Task<HRMS.Domain.Entities.ThirdPartyBill?> GetByIdAsync(Guid id)
    {
        return await _context.ThirdPartyBills.Include(b => b.Client).Include(b => b.Details).FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task<HRMS.Domain.Entities.ThirdPartyBill?> GetByBillNoAsync(string billNo)
    {
        return await _context.ThirdPartyBills.Include(b => b.Client).Include(b => b.Details).FirstOrDefaultAsync(b => b.BillNo == billNo);
    }

    public async Task<IEnumerable<HRMS.Domain.Entities.ThirdPartyBill>> GetByClientAndPeriodAsync(Guid clientId, int year, int month)
    {
        return await _context.ThirdPartyBills
            .Where(b => b.ClientId == clientId && b.Year == year && b.Month == month)
            .Include(b => b.Details)
            .ToListAsync();
    }

    public async Task<HRMS.Domain.Entities.ThirdPartyBill> CreateAsync(HRMS.Domain.Entities.ThirdPartyBill bill)
    {
        _context.ThirdPartyBills.Add(bill);
        await _context.SaveChangesAsync();
        return bill;
    }

    public async Task UpdateAsync(HRMS.Domain.Entities.ThirdPartyBill bill)
    {
        bill.UpdatedAt = DateTime.UtcNow;
        _context.ThirdPartyBills.Update(bill);
        await _context.SaveChangesAsync();
    }
}
