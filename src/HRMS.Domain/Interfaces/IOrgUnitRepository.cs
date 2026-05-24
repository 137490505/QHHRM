using HRMS.Domain.Entities;

namespace HRMS.Domain.Interfaces;

public interface IOrgUnitRepository
{
    Task<IEnumerable<OrgUnit>> GetAllAsync();
    Task<OrgUnit?> GetByIdAsync(Guid id);
    Task<OrgUnit?> GetByCodeAsync(string code);
    Task<IEnumerable<OrgUnit>> GetChildrenAsync(Guid parentId);
    Task<OrgUnit> CreateAsync(OrgUnit orgUnit);
    Task UpdateAsync(OrgUnit orgUnit);
    Task DeleteAsync(Guid id);
}
