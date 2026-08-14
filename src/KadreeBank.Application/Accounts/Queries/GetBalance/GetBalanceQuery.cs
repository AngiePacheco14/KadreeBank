using KadreeBank.Application.Accounts.Dtos;
using MediatR;

namespace KadreeBank.Application.Accounts.Queries.GetBalance;

public sealed record GetBalanceQuery(Guid AccountId) : IRequest<BalanceDto>;
