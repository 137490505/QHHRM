using HRMS.Application.DTOs;
using HRMS.Domain.Entities;
using HRMS.Domain.Enums;
using HRMS.Domain.Interfaces;

namespace HRMS.Application.Services;

public class EmployeeService
{
    private readonly IEmployeeRepository _employeeRepository;

    public EmployeeService(IEmployeeRepository employeeRepository)
    {
        _employeeRepository = employeeRepository;
    }

    public async Task<IEnumerable<EmployeeDto>> GetAllAsync()
    {
        var employees = await _employeeRepository.GetAllAsync();
        return employees.Select(MapToDto);
    }

    public async Task<EmployeeDto?> GetByIdAsync(Guid id)
    {
        var employee = await _employeeRepository.GetByIdAsync(id);
        return employee == null ? null : MapToDto(employee);
    }

    public async Task<EmployeeDto?> GetByEmployeeNoAsync(string employeeNo)
    {
        var employee = await _employeeRepository.GetByEmployeeNoAsync(employeeNo);
        return employee == null ? null : MapToDto(employee);
    }

    public async Task<IEnumerable<EmployeeDto>> GetByOrgUnitIdAsync(Guid orgUnitId)
    {
        var employees = await _employeeRepository.GetByOrgUnitIdAsync(orgUnitId);
        return employees.Select(MapToDto);
    }

    public async Task<IEnumerable<EmployeeDto>> GetByThirdPartyCompanyAsync(Guid companyId)
    {
        var employees = await _employeeRepository.GetByThirdPartyCompanyAsync(companyId);
        return employees.Select(MapToDto);
    }

    public async Task<EmployeeDto> CreateAsync(CreateEmployeeDto dto)
    {
        var employee = new Employee
        {
            EmployeeNo = dto.EmployeeNo,
            Name = dto.Name,
            Gender = dto.Gender,
            IdCard = dto.IdCard,
            Phone = dto.Phone,
            Email = dto.Email,
            EmployeeType = (EmployeeType)dto.EmployeeType,
            SalaryMode = (SalaryMode)dto.SalaryMode,
            OrgUnitId = dto.OrgUnitId,
            ThirdPartyCompanyId = dto.ThirdPartyCompanyId,
            JobTitle = dto.JobTitle,
            Level = dto.Level,
            Tags = dto.Tags ?? new List<string>(),
            HourlyRate = dto.HourlyRate,
            MonthlySalary = dto.MonthlySalary,
            PieceRatePrice = dto.PieceRatePrice,
            SocialSecurityBase = dto.SocialSecurityBase,
            HousingFundBase = dto.HousingFundBase,
            ContractType = dto.ContractType,
            ContractStartDate = dto.ContractStartDate,
            ContractEndDate = dto.ContractEndDate,
            ProbationDays = dto.ProbationDays,
            TrialDaysRemaining = dto.ProbationDays,
            HireDate = dto.HireDate
        };
        var created = await _employeeRepository.CreateAsync(employee);
        return MapToDto(created);
    }

    public async Task<EmployeeDto?> UpdateAsync(UpdateEmployeeDto dto)
    {
        var employee = await _employeeRepository.GetByIdAsync(dto.Id);
        if (employee == null) return null;

        employee.Name = dto.Name;
        employee.Gender = dto.Gender;
        employee.IdCard = dto.IdCard;
        employee.Phone = dto.Phone;
        employee.Email = dto.Email;
        employee.EmployeeType = (EmployeeType)dto.EmployeeType;
        employee.SalaryMode = (SalaryMode)dto.SalaryMode;
        employee.OrgUnitId = dto.OrgUnitId;
        employee.ThirdPartyCompanyId = dto.ThirdPartyCompanyId;
        employee.JobTitle = dto.JobTitle;
        employee.Level = dto.Level;
        employee.Tags = dto.Tags ?? employee.Tags;
        employee.HourlyRate = dto.HourlyRate;
        employee.MonthlySalary = dto.MonthlySalary;
        employee.PieceRatePrice = dto.PieceRatePrice;
        employee.SocialSecurityBase = dto.SocialSecurityBase;
        employee.HousingFundBase = dto.HousingFundBase;
        employee.ContractType = dto.ContractType;
        employee.ContractStartDate = dto.ContractStartDate;
        employee.ContractEndDate = dto.ContractEndDate;
        employee.ProbationDays = dto.ProbationDays;
        employee.HireDate = dto.HireDate;

        await _employeeRepository.UpdateAsync(employee);
        return MapToDto(employee);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var employee = await _employeeRepository.GetByIdAsync(id);
        if (employee == null) return false;
        await _employeeRepository.DeleteAsync(id);
        return true;
    }

    public async Task<bool> ToggleDismissAsync(Guid id)
    {
        var employee = await _employeeRepository.GetByIdAsync(id);
        if (employee == null) return false;

        employee.IsActive = !employee.IsActive;
        if (!employee.IsActive)
        {
            employee.DismissDate = DateTime.UtcNow;
            employee.DismissReason ??= "手动办理离职";
        }
        else
        {
            employee.DismissDate = null;
            employee.DismissReason = null;
        }

        await _employeeRepository.UpdateAsync(employee);
        return true;
    }

    public async Task<bool> BatchDismissAsync(BatchDismissDto dto)
    {
        foreach (var id in dto.Ids)
        {
            var employee = await _employeeRepository.GetByIdAsync(id);
            if (employee != null)
            {
                employee.IsActive = false;
                employee.DismissDate = dto.DismissDate;
                employee.DismissReason = dto.Reason;
                await _employeeRepository.UpdateAsync(employee);
            }
        }
        return true;
    }

    private static EmployeeDto MapToDto(Employee employee)
    {
        return new EmployeeDto
        {
            Id = employee.Id,
            EmployeeNo = employee.EmployeeNo,
            Name = employee.Name,
            Gender = employee.Gender,
            IdCard = employee.IdCard,
            Phone = employee.Phone,
            Email = employee.Email,
            EmployeeType = employee.EmployeeType.ToString(),
            SalaryMode = employee.SalaryMode.ToString(),
            OrgUnitId = employee.OrgUnitId,
            OrgUnitName = employee.OrgUnit?.Name ?? string.Empty,
            ThirdPartyCompanyId = employee.ThirdPartyCompanyId,
            ThirdPartyCompanyName = employee.ThirdPartyCompany?.Name,
            JobTitle = employee.JobTitle,
            Level = employee.Level,
            Tags = employee.Tags,
            HourlyRate = employee.HourlyRate,
            MonthlySalary = employee.MonthlySalary,
            PieceRatePrice = employee.PieceRatePrice,
            SocialSecurityBase = employee.SocialSecurityBase,
            HousingFundBase = employee.HousingFundBase,
            TrialEndDate = employee.TrialEndDate,
            TrialDaysRemaining = employee.TrialDaysRemaining,
            ProbationDays = employee.ProbationDays,
            ContractType = employee.ContractType,
            ContractStartDate = employee.ContractStartDate,
            ContractEndDate = employee.ContractEndDate,
            HireDate = employee.HireDate,
            IsBlacklisted = employee.IsBlacklisted,
            IsActive = employee.IsActive,
            DismissDate = employee.DismissDate,
            DismissReason = employee.DismissReason
        };
    }
}
