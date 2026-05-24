using System.ComponentModel.DataAnnotations;

namespace HRMS.Application.DTOs;

public class CustomerDto
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
    public string? InvoiceTitle { get; set; }
    public string? Remark { get; set; }
    public bool IsActive { get; set; }
}

public class CreateCustomerDto
{
    [Required(ErrorMessage = "客户编码不能为空")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "客户名称不能为空")]
    public string Name { get; set; } = string.Empty;

    public string? ShortName { get; set; }
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? TaxNo { get; set; }
    public string? Address { get; set; }
    public string? InvoiceTitle { get; set; }
    public string? Remark { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateCustomerDto : CreateCustomerDto
{
    public Guid Id { get; set; }
}
