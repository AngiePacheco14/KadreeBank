namespace KadreeBank.Application.Reports.Dtos;

public sealed record CustomerTransactionCountDto(
    Guid CustomerId,
    string CustomerName,
    int TransactionCount);
