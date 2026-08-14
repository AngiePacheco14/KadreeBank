using KadreeBank.Application.Customers.Dtos;
using KadreeBank.Domain.Enums;
using MediatR;

namespace KadreeBank.Application.Customers.Commands.CreateCustomer;

public sealed record CreateCustomerCommand(
    CustomerType Type,
    string FullName,
    string DocumentNumber) : IRequest<CustomerDto>;
