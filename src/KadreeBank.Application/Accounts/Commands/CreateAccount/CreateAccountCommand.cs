using KadreeBank.Application.Accounts.Dtos;
using KadreeBank.Domain.Enums;
using MediatR;

namespace KadreeBank.Application.Accounts.Commands.CreateAccount;

public sealed record CreateAccountCommand(
    Guid CustomerId,
    AccountType Type,
    string OriginCity) : IRequest<AccountDto>;
