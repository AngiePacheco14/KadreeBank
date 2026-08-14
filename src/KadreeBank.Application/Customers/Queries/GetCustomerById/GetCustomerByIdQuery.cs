using KadreeBank.Application.Customers.Dtos;
using MediatR;

namespace KadreeBank.Application.Customers.Queries.GetCustomerById;

public sealed record GetCustomerByIdQuery(Guid CustomerId) : IRequest<CustomerDto>;
