using KadreeBank.Application.Accounts.Dtos;
using MediatR;

namespace KadreeBank.Application.Accounts.Commands.Deposit;

public sealed record DepositCommand(
    Guid AccountId,
    decimal Amount,
    string City) : IRequest<BalanceDto>;
