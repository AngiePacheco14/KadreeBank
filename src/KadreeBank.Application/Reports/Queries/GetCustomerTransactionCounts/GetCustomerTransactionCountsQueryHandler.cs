using KadreeBank.Application.Reports.Dtos;
using KadreeBank.Application.Services;
using MediatR;

namespace KadreeBank.Application.Reports.Queries.GetCustomerTransactionCounts;

public sealed class GetCustomerTransactionCountsQueryHandler(IReportService reportService)
    : IRequestHandler<GetCustomerTransactionCountsQuery, IReadOnlyList<CustomerTransactionCountDto>>
{
    public Task<IReadOnlyList<CustomerTransactionCountDto>> Handle(
        GetCustomerTransactionCountsQuery request, CancellationToken cancellationToken) =>
        reportService.GetCustomerTransactionCountsAsync(request.Year, request.Month, cancellationToken);
}
