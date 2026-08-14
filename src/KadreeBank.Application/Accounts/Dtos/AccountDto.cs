using KadreeBank.Domain.Enums;

namespace KadreeBank.Application.Accounts.Dtos;

public sealed record AccountDto(
    Guid Id,
    Guid CustomerId,
    string AccountNumber,
    AccountType Type,
    decimal Balance,
    string OriginCity,
    DateTime CreatedAt);
