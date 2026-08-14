using KadreeBank.Application.Reports.Dtos;
using MediatR;

namespace KadreeBank.Application.Reports.Queries.GetOutOfCityWithdrawals;

public sealed record GetOutOfCityWithdrawalsQuery(decimal MinAmount = 1_000_000m)
    : IRequest<IReadOnlyList<OutOfCityWithdrawalDto>>;
