using KadreeBank.Application.Customers.Dtos;
using KadreeBank.Application.Services;
using MediatR;

namespace KadreeBank.Application.Customers.Commands.CreateCustomer;

public sealed class CreateCustomerCommandHandler(ICustomerService customerService)
    : IRequestHandler<CreateCustomerCommand, CustomerDto>
{
    public Task<CustomerDto> Handle(CreateCustomerCommand request, CancellationToken cancellationToken) =>
        customerService.CreateCustomerAsync(request.Type, request.FullName, request.DocumentNumber, cancellationToken);
}
