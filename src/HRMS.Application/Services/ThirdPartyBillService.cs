using HRMS.Application.DTOs;
using HRMS.Domain.Entities;
using HRMS.Domain.Enums;
using HRMS.Domain.Interfaces;

namespace HRMS.Application.Services;

public class ThirdPartyBillService
{
    private readonly IThirdPartyBillRepository _billRepository;
    private readonly ITimesheetRepository _timesheetRepository;

    public ThirdPartyBillService(
        IThirdPartyBillRepository billRepository,
        ITimesheetRepository timesheetRepository)
    {
        _billRepository = billRepository;
        _timesheetRepository = timesheetRepository;
    }

    public async Task<IEnumerable<ThirdPartyBillDto>> GetAllAsync()
    {
        var bills = await _billRepository.GetAllAsync();
        return bills.Select(MapToDto);
    }

    public async Task<ThirdPartyBillDto?> GetByIdAsync(Guid id)
    {
        var bill = await _billRepository.GetByIdAsync(id);
        return bill == null ? null : MapToDto(bill);
    }

    public async Task<ThirdPartyBillDto?> GenerateBillAsync(GenerateBillDto dto)
    {
        var timesheets = await _timesheetRepository.GetByOrgUnitAndDateRangeAsync(
            dto.ClientId,
            new DateTime(dto.Year, dto.Month, 1),
            new DateTime(dto.Year, dto.Month, 1).AddMonths(1).AddDays(-1));

        var approvedTimesheets = timesheets.Where(t => t.ApprovalStatus == ApprovalStatus.Approved).ToList();

        if (!approvedTimesheets.Any()) return null;

        var totalHours = approvedTimesheets.Sum(t => t.WorkingHours);
        var totalWages = approvedTimesheets.Sum(t => t.WorkingHours * (t.Employee?.HourlyRate ?? 0));

        decimal managementFee = dto.ManagementFeeType switch
        {
            "Fixed" => dto.ManagementFeeRate,
            "PercentOfWages" => totalWages * dto.ManagementFeeRate / 100,
            "PercentOfHours" => totalHours * dto.ManagementFeeRate / 100,
            _ => 0
        };

        var bill = new ThirdPartyBill
        {
            BillNo = GenerateBillNo(dto.Year, dto.Month),
            ClientId = dto.ClientId,
            Year = dto.Year,
            Month = dto.Month,
            TotalWages = totalWages,
            ManagementFee = managementFee,
            ManagementFeeRate = dto.ManagementFeeRate,
            ManagementFeeType = dto.ManagementFeeType,
            OtherFees = dto.OtherFees,
            TotalAmount = totalWages + managementFee + dto.OtherFees,
            Status = BillStatus.Draft
        };

        var tagGroups = approvedTimesheets
            .Where(t => t.Employee?.Tags != null)
            .SelectMany(t => t.Employee!.Tags.Select(tag => new { Tag = tag, Timesheet = t }))
            .GroupBy(x => x.Tag);

        foreach (var group in tagGroups)
        {
            bill.Details.Add(new ThirdPartyBillDetail
            {
                Tag = group.Key,
                TotalWages = group.Sum(x => x.Timesheet.WorkingHours * (x.Timesheet.Employee?.HourlyRate ?? 0)),
                TotalManagementFee = managementFee * group.Sum(x => x.Timesheet.WorkingHours) / totalHours,
                EmployeeCount = group.Select(x => x.Timesheet.EmployeeId).Distinct().Count(),
                TotalHours = group.Sum(x => x.Timesheet.WorkingHours)
            });
        }

        var created = await _billRepository.CreateAsync(bill);
        return MapToDto(created);
    }

    private static string GenerateBillNo(int year, int month)
    {
        return $"TPB{year}{month:D2}{DateTime.Now:ddHHmmss}";
    }

    private static ThirdPartyBillDto MapToDto(ThirdPartyBill bill)
    {
        return new ThirdPartyBillDto
        {
            Id = bill.Id,
            BillNo = bill.BillNo,
            ClientId = bill.ClientId,
            ClientName = bill.Client?.Name ?? string.Empty,
            Year = bill.Year,
            Month = bill.Month,
            TotalWages = bill.TotalWages,
            ManagementFee = bill.ManagementFee,
            ManagementFeeType = bill.ManagementFeeType,
            OtherFees = bill.OtherFees,
            TotalAmount = bill.TotalAmount,
            Status = bill.Status.ToString(),
            Details = bill.Details.Select(d => new ThirdPartyBillDetailDto
            {
                Tag = d.Tag,
                TotalWages = d.TotalWages,
                TotalManagementFee = d.TotalManagementFee,
                EmployeeCount = d.EmployeeCount,
                TotalHours = d.TotalHours
            }).ToList()
        };
    }
}
