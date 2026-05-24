using HRMS.Application.DTOs;
using HRMS.Domain.Entities;
using HRMS.Domain.Enums;
using HRMS.Domain.Interfaces;

namespace HRMS.Application.Services;

public class SupplierService
{
    private readonly ISupplierRepository _supplierRepository;
    private readonly IOrgUnitRepository _orgUnitRepository;
    private readonly IEmployeeRepository _employeeRepository;

    public SupplierService(
        ISupplierRepository supplierRepository,
        IOrgUnitRepository orgUnitRepository,
        IEmployeeRepository employeeRepository)
    {
        _supplierRepository = supplierRepository;
        _orgUnitRepository = orgUnitRepository;
        _employeeRepository = employeeRepository;
    }

    public async Task<IEnumerable<SupplierDto>> GetAllAsync()
    {
        var suppliers = await _supplierRepository.GetAllAsync();
        var employeeCounts = await GetEmployeeCountsAsync();
        return suppliers.Select(supplier => MapToDto(supplier, employeeCounts));
    }

    public async Task<SupplierDto?> GetByIdAsync(Guid id)
    {
        var supplier = await _supplierRepository.GetByIdAsync(id);
        if (supplier == null)
        {
            return null;
        }

        var employeeCounts = await GetEmployeeCountsAsync();
        return MapToDto(supplier, employeeCounts);
    }

    public async Task<SupplierDto> CreateAsync(CreateSupplierDto dto)
    {
        await EnsureCodeUniqueAsync(dto.Code, null);

        var supplier = new Supplier
        {
            Code = dto.Code.Trim(),
            Name = dto.Name.Trim(),
            ShortName = dto.ShortName?.Trim(),
            ContactPerson = dto.ContactPerson?.Trim(),
            Phone = dto.Phone?.Trim(),
            Email = dto.Email?.Trim(),
            TaxNo = dto.TaxNo?.Trim(),
            Address = dto.Address?.Trim(),
            BankName = dto.BankName?.Trim(),
            BankAccount = dto.BankAccount?.Trim(),
            PaymentTermDays = Math.Max(dto.PaymentTermDays, 0),
            Remark = dto.Remark?.Trim(),
            IsActive = dto.IsActive
        };

        var created = await _supplierRepository.CreateAsync(supplier);
        await UpsertSupplierOrgUnitAsync(created);

        return MapToDto(created, new Dictionary<Guid, int>());
    }

    public async Task<SupplierDto?> UpdateAsync(UpdateSupplierDto dto)
    {
        var supplier = await _supplierRepository.GetByIdAsync(dto.Id);
        if (supplier == null)
        {
            return null;
        }

        await EnsureCodeUniqueAsync(dto.Code, dto.Id);

        supplier.Code = dto.Code.Trim();
        supplier.Name = dto.Name.Trim();
        supplier.ShortName = dto.ShortName?.Trim();
        supplier.ContactPerson = dto.ContactPerson?.Trim();
        supplier.Phone = dto.Phone?.Trim();
        supplier.Email = dto.Email?.Trim();
        supplier.TaxNo = dto.TaxNo?.Trim();
        supplier.Address = dto.Address?.Trim();
        supplier.BankName = dto.BankName?.Trim();
        supplier.BankAccount = dto.BankAccount?.Trim();
        supplier.PaymentTermDays = Math.Max(dto.PaymentTermDays, 0);
        supplier.Remark = dto.Remark?.Trim();
        supplier.IsActive = dto.IsActive;

        await _supplierRepository.UpdateAsync(supplier);
        await UpsertSupplierOrgUnitAsync(supplier);

        var employeeCounts = await GetEmployeeCountsAsync();
        return MapToDto(supplier, employeeCounts);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var supplier = await _supplierRepository.GetByIdAsync(id);
        if (supplier == null)
        {
            return false;
        }

        var employees = await _employeeRepository.GetByThirdPartyCompanyAsync(id);
        if (employees.Any())
        {
            throw new InvalidOperationException("该供应商下仍有关联三方员工，无法删除");
        }

        var supplierOrgUnit = await _orgUnitRepository.GetByIdAsync(id);
        if (supplierOrgUnit != null)
        {
            await _orgUnitRepository.DeleteAsync(id);
        }

        await _supplierRepository.DeleteAsync(supplier);
        return true;
    }

    private async Task EnsureCodeUniqueAsync(string code, Guid? currentId)
    {
        var normalizedCode = code.Trim();
        var existing = await _supplierRepository.GetByCodeAsync(normalizedCode);
        if (existing != null && existing.Id != currentId)
        {
            throw new InvalidOperationException("供应商编码已存在");
        }

        var existingOrgUnit = await _orgUnitRepository.GetByCodeAsync(normalizedCode);
        if (existingOrgUnit != null && existingOrgUnit.Id != currentId)
        {
            throw new InvalidOperationException("供应商编码已被组织单元占用");
        }
    }

    private async Task UpsertSupplierOrgUnitAsync(Supplier supplier)
    {
        var orgUnit = await _orgUnitRepository.GetByIdAsync(supplier.Id);
        if (orgUnit == null)
        {
            orgUnit = new OrgUnit
            {
                Id = supplier.Id,
                Code = supplier.Code,
                Name = supplier.Name,
                Level = OrgLevel.Supplier,
                IsActive = supplier.IsActive,
                CreatedAt = DateTime.UtcNow
            };
            await _orgUnitRepository.CreateAsync(orgUnit);
        }
        else
        {
            orgUnit.Code = supplier.Code;
            orgUnit.Name = supplier.Name;
            orgUnit.Level = OrgLevel.Supplier;
            orgUnit.ParentId = null;
            orgUnit.IsActive = supplier.IsActive;
            await _orgUnitRepository.UpdateAsync(orgUnit);
        }
    }

    private async Task<Dictionary<Guid, int>> GetEmployeeCountsAsync()
    {
        var counts = new Dictionary<Guid, int>();
        var suppliers = await _supplierRepository.GetAllAsync();
        foreach (var supplier in suppliers)
        {
            var employees = await _employeeRepository.GetByThirdPartyCompanyAsync(supplier.Id);
            counts[supplier.Id] = employees.Count();
        }

        return counts;
    }

    private static SupplierDto MapToDto(Supplier supplier, IReadOnlyDictionary<Guid, int> employeeCounts)
    {
        return new SupplierDto
        {
            Id = supplier.Id,
            Code = supplier.Code,
            Name = supplier.Name,
            ShortName = supplier.ShortName,
            ContactPerson = supplier.ContactPerson,
            Phone = supplier.Phone,
            Email = supplier.Email,
            TaxNo = supplier.TaxNo,
            Address = supplier.Address,
            BankName = supplier.BankName,
            BankAccount = supplier.BankAccount,
            PaymentTermDays = supplier.PaymentTermDays,
            Remark = supplier.Remark,
            IsActive = supplier.IsActive,
            EmployeeCount = employeeCounts.TryGetValue(supplier.Id, out var count) ? count : 0
        };
    }
}
