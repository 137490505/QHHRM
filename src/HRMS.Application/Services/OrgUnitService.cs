using HRMS.Application.DTOs;
using HRMS.Domain.Entities;
using HRMS.Domain.Enums;
using HRMS.Domain.Interfaces;

namespace HRMS.Application.Services;

public class OrgUnitService
{
    private readonly IOrgUnitRepository _orgUnitRepository;
    private readonly IEmployeeRepository _employeeRepository;

    public OrgUnitService(IOrgUnitRepository orgUnitRepository, IEmployeeRepository employeeRepository)
    {
        _orgUnitRepository = orgUnitRepository;
        _employeeRepository = employeeRepository;
    }

    public async Task<IEnumerable<OrgUnitDto>> GetAllAsync()
    {
        var orgUnits = await _orgUnitRepository.GetAllAsync();
        var managerMap = await BuildManagerMapAsync();
        return orgUnits.Select(orgUnit => MapToDto(orgUnit, managerMap));
    }

    public async Task<OrgUnitDto?> GetByIdAsync(Guid id)
    {
        var orgUnit = await _orgUnitRepository.GetByIdAsync(id);
        if (orgUnit == null) return null;

        var managerMap = await BuildManagerMapAsync();
        return MapToDto(orgUnit, managerMap);
    }

    public async Task<OrgUnitDto?> GetByCodeAsync(string code)
    {
        var orgUnit = await _orgUnitRepository.GetByCodeAsync(code);
        if (orgUnit == null) return null;

        var managerMap = await BuildManagerMapAsync();
        return MapToDto(orgUnit, managerMap);
    }

    public async Task<IEnumerable<OrgUnitDto>> GetTreeAsync()
    {
        var all = await _orgUnitRepository.GetAllAsync();
        var managerMap = await BuildManagerMapAsync();
        var dtoMap = all.Select(orgUnit => MapToDto(orgUnit, managerMap)).ToDictionary(d => d.Id);
        var roots = new List<OrgUnitDto>();

        foreach (var dto in dtoMap.Values)
        {
            if (dto.ParentId.HasValue && dtoMap.TryGetValue(dto.ParentId.Value, out var parent))
            {
                parent.Children.Add(dto);
            }
            else
            {
                roots.Add(dto);
            }
        }
        return roots;
    }

    public async Task<IEnumerable<OrgUnitDto>> GetChildrenAsync(Guid parentId)
    {
        var children = await _orgUnitRepository.GetChildrenAsync(parentId);
        var managerMap = await BuildManagerMapAsync();
        return children.Select(child => MapToDto(child, managerMap));
    }

    public async Task<OrgUnitDto> CreateAsync(CreateOrgUnitDto dto)
    {
        var orgUnit = new OrgUnit
        {
            Code = dto.Code,
            Name = dto.Name,
            Level = (OrgLevel)dto.Level,
            ParentId = dto.ParentId,
            ManagerId = dto.ManagerId
        };
        var created = await _orgUnitRepository.CreateAsync(orgUnit);
        var managerMap = await BuildManagerMapAsync();
        return MapToDto(created, managerMap);
    }

    public async Task<OrgUnitDto?> UpdateAsync(UpdateOrgUnitDto dto)
    {
        var orgUnit = await _orgUnitRepository.GetByIdAsync(dto.Id);
        if (orgUnit == null) return null;

        orgUnit.Code = dto.Code;
        orgUnit.Name = dto.Name;
        orgUnit.Level = (OrgLevel)dto.Level;
        orgUnit.ParentId = dto.ParentId;
        orgUnit.ManagerId = dto.ManagerId;
        orgUnit.IsActive = dto.IsActive;

        await _orgUnitRepository.UpdateAsync(orgUnit);
        var managerMap = await BuildManagerMapAsync();
        return MapToDto(orgUnit, managerMap);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var orgUnit = await _orgUnitRepository.GetByIdAsync(id);
        if (orgUnit == null) return false;

        // 检查是否有子节点
        var children = await _orgUnitRepository.GetChildrenAsync(id);
        if (children.Any())
        {
            throw new Exception("该组织下有子节点，无法删除");
        }

        // 检查是否有关联员工
        var employees = await _employeeRepository.GetByOrgUnitIdAsync(id);
        if (employees.Any())
        {
            throw new Exception("该组织下有员工，无法删除");
        }

        await _orgUnitRepository.DeleteAsync(id);
        return true;
    }

    public async Task<bool> BatchEnableAsync(IEnumerable<Guid> ids)
    {
        foreach (var id in ids)
        {
            var orgUnit = await _orgUnitRepository.GetByIdAsync(id);
            if (orgUnit != null)
            {
                orgUnit.IsActive = true;
                await _orgUnitRepository.UpdateAsync(orgUnit);
            }
        }
        return true;
    }

    public async Task<bool> BatchDisableAsync(IEnumerable<Guid> ids)
    {
        foreach (var id in ids)
        {
            var orgUnit = await _orgUnitRepository.GetByIdAsync(id);
            if (orgUnit != null)
            {
                orgUnit.IsActive = false;
                await _orgUnitRepository.UpdateAsync(orgUnit);
            }
        }
        return true;
    }

    public async Task<bool> BatchUpdateAsync(BatchUpdateOrgUnitDto dto)
    {
        if (dto.Ids == null || dto.Ids.Count == 0)
        {
            throw new Exception("请选择要修改的组织单元");
        }

        if (!dto.Level.HasValue && !dto.IsActive.HasValue)
        {
            throw new Exception("请至少选择一个修改项");
        }

        foreach (var id in dto.Ids.Distinct())
        {
            var orgUnit = await _orgUnitRepository.GetByIdAsync(id);
            if (orgUnit == null)
            {
                continue;
            }

            if (dto.Level.HasValue)
            {
                orgUnit.Level = (OrgLevel)dto.Level.Value;
            }

            if (dto.IsActive.HasValue)
            {
                orgUnit.IsActive = dto.IsActive.Value;
            }

            await _orgUnitRepository.UpdateAsync(orgUnit);
        }

        return true;
    }

    public async Task<bool> ToggleStatusAsync(Guid id)
    {
        var orgUnit = await _orgUnitRepository.GetByIdAsync(id);
        if (orgUnit == null) return false;

        orgUnit.IsActive = !orgUnit.IsActive;
        await _orgUnitRepository.UpdateAsync(orgUnit);
        return true;
    }

    private async Task<Dictionary<string, string>> BuildManagerMapAsync()
    {
        var employees = await _employeeRepository.GetAllAsync();
        return employees
            .Where(employee => employee.Id != Guid.Empty && !string.IsNullOrWhiteSpace(employee.Name))
            .GroupBy(employee => employee.Id.ToString())
            .ToDictionary(group => group.Key, group => group.First().Name);
    }

    private static OrgUnitDto MapToDto(OrgUnit orgUnit, IReadOnlyDictionary<string, string> managerMap)
    {
        return new OrgUnitDto
        {
            Id = orgUnit.Id,
            Code = orgUnit.Code,
            Name = orgUnit.Name,
            Level = (int)orgUnit.Level,
            ParentId = orgUnit.ParentId,
            ParentName = orgUnit.Parent?.Name,
            ManagerId = orgUnit.ManagerId,
            Manager = !string.IsNullOrWhiteSpace(orgUnit.ManagerId) && managerMap.TryGetValue(orgUnit.ManagerId, out var managerName)
                ? managerName
                : null,
            IsActive = orgUnit.IsActive
        };
    }
}
