using System.ComponentModel.DataAnnotations;

namespace HRMS.Application.DTOs;

public class SupplierDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ShortName { get; set; }
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? TaxNo { get; set; }
    public string? Address { get; set; }
    public string? BankName { get; set; }
    public string? BankAccount { get; set; }
    public int PaymentTermDays { get; set; }
    public string? Remark { get; set; }
    public bool IsActive { get; set; }
    public int EmployeeCount { get; set; }
}

public class CreateSupplierDto
{
    [Required(ErrorMessage = "供应商编码不能为空")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "供应商名称不能为空")]
    public string Name { get; set; } = string.Empty;

    public string? ShortName { get; set; }
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? TaxNo { get; set; }
    public string? Address { get; set; }
    public string? BankName { get; set; }
    public string? BankAccount { get; set; }
    public int PaymentTermDays { get; set; }
    public string? Remark { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateSupplierDto : CreateSupplierDto
{
    public Guid Id { get; set; }
}
