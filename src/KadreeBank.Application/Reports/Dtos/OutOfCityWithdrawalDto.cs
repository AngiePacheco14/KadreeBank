namespace KadreeBank.Application.Reports.Dtos;

public sealed record OutOfCityWithdrawalDto(
    Guid CustomerId,
    string CustomerName,
    decimal TotalWithdrawn);
