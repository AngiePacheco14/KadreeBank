using KadreeBank.Application.Reports.Dtos;
using KadreeBank.Application.Reports.Interfaces;

namespace KadreeBank.Application.Services;

public class ReportService(IReportQueries reportQueries) : IReportService
{
    public Task<IReadOnlyList<CustomerTransactionCountDto>> GetCustomerTransactionCountsAsync(
        int year, int month, CancellationToken cancellationToken = default) =>
        reportQueries.GetCustomerTransactionCountsAsync(year, month, cancellationToken);

    public Task<IReadOnlyList<OutOfCityWithdrawalDto>> GetOutOfCityWithdrawalsAsync(
        decimal minAmount, CancellationToken cancellationToken = default) =>
        reportQueries.GetOutOfCityWithdrawalsAsync(minAmount, cancellationToken);
}
