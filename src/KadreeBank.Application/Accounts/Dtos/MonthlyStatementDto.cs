namespace KadreeBank.Application.Accounts.Dtos;

public sealed record MonthlyStatementDto(
    Guid AccountId,
    int Year,
    int Month,
    decimal OpeningBalance,
    decimal ClosingBalance,
    IReadOnlyList<TransactionDto> Movements);
