using HRMS.Domain.Entities;
using HRMS.Domain.Interfaces;
using HRMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Infrastructure.Repositories;

public class OrgUnitRepository : IOrgUnitRepository
{
    private readonly HrmsDbContext _context;

    public OrgUnitRepository(HrmsDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<OrgUnit>> GetAllAsync()
    {
        return await _context.OrgUnits
            .Include(o => o.Parent)
            .OrderBy(o => o.CreatedAt)
            .ThenBy(o => o.Code)
            .ToListAsync();
    }

    public async Task<OrgUnit?> GetByIdAsync(Guid id)
    {
        return await _context.OrgUnits.FindAsync(id);
    }

    public async Task<OrgUnit?> GetByCodeAsync(string code)
    {
        return await _context.OrgUnits.FirstOrDefaultAsync(o => o.Code == code);
    }

    public async Task<IEnumerable<OrgUnit>> GetChildrenAsync(Guid parentId)
    {
        return await _context.OrgUnits
            .Where(o => o.ParentId == parentId)
            .OrderBy(o => o.CreatedAt)
            .ThenBy(o => o.Code)
            .ToListAsync();
    }

    public async Task<OrgUnit> CreateAsync(OrgUnit orgUnit)
    {
        _context.OrgUnits.Add(orgUnit);
        await _context.SaveChangesAsync();
        return orgUnit;
    }

    public async Task UpdateAsync(OrgUnit orgUnit)
    {
        orgUnit.UpdatedAt = DateTime.UtcNow;
        _context.OrgUnits.Update(orgUnit);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var orgUnit = await _context.OrgUnits.FindAsync(id);
        if (orgUnit != null)
        {
            _context.OrgUnits.Remove(orgUnit);
            await _context.SaveChangesAsync();
        }
    }
}
