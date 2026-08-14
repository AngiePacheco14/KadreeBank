using KadreeBank.Application.Accounts.Dtos;
using MediatR;

namespace KadreeBank.Application.Accounts.Commands.Withdraw;

public sealed record WithdrawCommand(
    Guid AccountId,
    decimal Amount,
    string City) : IRequest<BalanceDto>;
