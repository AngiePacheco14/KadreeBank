using KadreeBank.Application.Reports.Dtos;

namespace KadreeBank.Application.Services;

public interface IReportService
{
    Task<IReadOnlyList<CustomerTransactionCountDto>> GetCustomerTransactionCountsAsync(
        int year, int month, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OutOfCityWithdrawalDto>> GetOutOfCityWithdrawalsAsync(
        decimal minAmount, CancellationToken cancellationToken = default);
}
