using HRMS.Application.DTOs;
using HRMS.Domain.Entities;
using HRMS.Domain.Interfaces;

namespace HRMS.Application.Services;

public class CustomerService
{
    private readonly ICustomerRepository _customerRepository;

    public CustomerService(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<IEnumerable<CustomerDto>> GetAllAsync()
    {
        var customers = await _customerRepository.GetAllAsync();
        return customers.Select(MapToDto);
    }

    public async Task<CustomerDto?> GetByIdAsync(Guid id)
    {
        var customer = await _customerRepository.GetByIdAsync(id);
        return customer == null ? null : MapToDto(customer);
    }

    public async Task<CustomerDto> CreateAsync(CreateCustomerDto dto)
    {
        await EnsureCodeUniqueAsync(dto.Code, null);

        var customer = new Customer
        {
            Code = dto.Code.Trim(),
            Name = dto.Name.Trim(),
            ShortName = dto.ShortName?.Trim(),
            ContactPerson = dto.ContactPerson?.Trim(),
            Phone = dto.Phone?.Trim(),
            Email = dto.Email?.Trim(),
            TaxNo = dto.TaxNo?.Trim(),
            Address = dto.Address?.Trim(),
            InvoiceTitle = dto.InvoiceTitle?.Trim(),
            Remark = dto.Remark?.Trim(),
            IsActive = dto.IsActive
        };

        var created = await _customerRepository.CreateAsync(customer);
        return MapToDto(created);
    }

    public async Task<CustomerDto?> UpdateAsync(UpdateCustomerDto dto)
    {
        var customer = await _customerRepository.GetByIdAsync(dto.Id);
        if (customer == null)
        {
            return null;
        }

        await EnsureCodeUniqueAsync(dto.Code, dto.Id);

        customer.Code = dto.Code.Trim();
        customer.Name = dto.Name.Trim();
        customer.ShortName = dto.ShortName?.Trim();
        customer.ContactPerson = dto.ContactPerson?.Trim();
        customer.Phone = dto.Phone?.Trim();
        customer.Email = dto.Email?.Trim();
        customer.TaxNo = dto.TaxNo?.Trim();
        customer.Address = dto.Address?.Trim();
        customer.InvoiceTitle = dto.InvoiceTitle?.Trim();
        customer.Remark = dto.Remark?.Trim();
        customer.IsActive = dto.IsActive;

        await _customerRepository.UpdateAsync(customer);
        return MapToDto(customer);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var customer = await _customerRepository.GetByIdAsync(id);
        if (customer == null)
        {
            return false;
        }

        await _customerRepository.DeleteAsync(customer);
        return true;
    }

    private async Task EnsureCodeUniqueAsync(string code, Guid? currentId)
    {
        var existing = await _customerRepository.GetByCodeAsync(code.Trim());
        if (existing != null && existing.Id != currentId)
        {
            throw new InvalidOperationException("客户编码已存在");
        }
    }

    private static CustomerDto MapToDto(Customer customer)
    {
        return new CustomerDto
        {
            Id = customer.Id,
            Code = customer.Code,
            Name = customer.Name,
            ShortName = customer.ShortName,
            ContactPerson = customer.ContactPerson,
            Phone = customer.Phone,
            Email = customer.Email,
            TaxNo = customer.TaxNo,
            Address = customer.Address,
            InvoiceTitle = customer.InvoiceTitle,
            Remark = customer.Remark,
            IsActive = customer.IsActive
        };
    }
}
