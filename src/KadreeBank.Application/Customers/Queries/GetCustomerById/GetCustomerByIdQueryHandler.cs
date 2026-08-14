using KadreeBank.Application.Customers.Dtos;
using KadreeBank.Application.Services;
using MediatR;

namespace KadreeBank.Application.Customers.Queries.GetCustomerById;

public sealed class GetCustomerByIdQueryHandler(ICustomerService customerService)
    : IRequestHandler<GetCustomerByIdQuery, CustomerDto>
{
    public Task<CustomerDto> Handle(GetCustomerByIdQuery request, CancellationToken cancellationToken) =>
        customerService.GetByIdAsync(request.CustomerId, cancellationToken);
}
