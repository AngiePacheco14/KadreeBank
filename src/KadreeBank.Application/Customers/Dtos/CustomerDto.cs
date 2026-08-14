using KadreeBank.Domain.Enums;

namespace KadreeBank.Application.Customers.Dtos;

public sealed record CustomerDto(
    Guid Id,
    string FullName,
    string DocumentNumber,
    CustomerType Type,
    DateTime CreatedAt);
