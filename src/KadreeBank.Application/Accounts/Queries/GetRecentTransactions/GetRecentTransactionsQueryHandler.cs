using KadreeBank.Application.Accounts.Dtos;
using KadreeBank.Application.Services;
using MediatR;

namespace KadreeBank.Application.Accounts.Queries.GetRecentTransactions;

public sealed class GetRecentTransactionsQueryHandler(IAccountService accountService)
    : IRequestHandler<GetRecentTransactionsQuery, IReadOnlyList<TransactionDto>>
{
    public Task<IReadOnlyList<TransactionDto>> Handle(
        GetRecentTransactionsQuery request, CancellationToken cancellationToken) =>
        accountService.GetRecentTransactionsAsync(request.AccountId, request.Count, cancellationToken);
}
