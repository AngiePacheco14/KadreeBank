using KadreeBank.Application.Customers.Dtos;
using KadreeBank.Application.Services;
using MediatR;

namespace KadreeBank.Application.Customers.Queries.GetCustomers;

public sealed class GetCustomersQueryHandler(ICustomerService customerService)
    : IRequestHandler<GetCustomersQuery, IReadOnlyList<CustomerDto>>
{
    public Task<IReadOnlyList<CustomerDto>> Handle(GetCustomersQuery request, CancellationToken cancellationToken) =>
        customerService.GetAllAsync(request.Type, cancellationToken);
}
