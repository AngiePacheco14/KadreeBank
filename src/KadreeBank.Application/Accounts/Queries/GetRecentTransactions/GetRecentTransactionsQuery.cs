using KadreeBank.Application.Accounts.Dtos;
using MediatR;

namespace KadreeBank.Application.Accounts.Queries.GetRecentTransactions;

public sealed record GetRecentTransactionsQuery(Guid AccountId, int Count = 10) : IRequest<IReadOnlyList<TransactionDto>>;
