using KadreeBank.Application.Customers.Dtos;
using KadreeBank.Domain.Enums;
using MediatR;

namespace KadreeBank.Application.Customers.Queries.GetCustomers;

public sealed record GetCustomersQuery(CustomerType? Type) : IRequest<IReadOnlyList<CustomerDto>>;
