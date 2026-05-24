using HRMS.Domain.Entities;
using HRMS.Domain.Enums;

namespace HRMS.Domain.Interfaces;

public interface IThirdPartyBillRepository
{
    Task<IEnumerable<ThirdPartyBill>> GetAllAsync();
    Task<ThirdPartyBill?> GetByIdAsync(Guid id);
    Task<ThirdPartyBill?> GetByBillNoAsync(string billNo);
    Task<IEnumerable<ThirdPartyBill>> GetByClientAndPeriodAsync(Guid clientId, int year, int month);
    Task<ThirdPartyBill> CreateAsync(ThirdPartyBill bill);
    Task UpdateAsync(ThirdPartyBill bill);
}
