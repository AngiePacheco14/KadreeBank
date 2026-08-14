using KadreeBank.Domain.Enums;

namespace KadreeBank.Application.Accounts.Dtos;

public sealed record TransactionDto(
    Guid Id,
    Guid AccountId,
    TransactionType Type,
    decimal Amount,
    string City,
    decimal BalanceAfter,
    DateTime Timestamp);
