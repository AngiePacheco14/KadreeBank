using KadreeBank.Application.Reports.Dtos;
using KadreeBank.Application.Services;
using MediatR;

namespace KadreeBank.Application.Reports.Queries.GetOutOfCityWithdrawals;

public sealed class GetOutOfCityWithdrawalsQueryHandler(IReportService reportService)
    : IRequestHandler<GetOutOfCityWithdrawalsQuery, IReadOnlyList<OutOfCityWithdrawalDto>>
{
    public Task<IReadOnlyList<OutOfCityWithdrawalDto>> Handle(
        GetOutOfCityWithdrawalsQuery request, CancellationToken cancellationToken) =>
        reportService.GetOutOfCityWithdrawalsAsync(request.MinAmount, cancellationToken);
}
