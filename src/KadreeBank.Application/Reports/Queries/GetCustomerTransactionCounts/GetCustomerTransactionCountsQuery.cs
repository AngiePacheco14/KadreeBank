using KadreeBank.Application.Reports.Dtos;
using MediatR;

namespace KadreeBank.Application.Reports.Queries.GetCustomerTransactionCounts;

public sealed record GetCustomerTransactionCountsQuery(int Year, int Month)
    : IRequest<IReadOnlyList<CustomerTransactionCountDto>>;
