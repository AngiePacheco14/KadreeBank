using KadreeBank.Application.Common.Mappings;
using KadreeBank.Application.Customers.Dtos;
using KadreeBank.Domain.Entities;
using KadreeBank.Domain.Enums;
using KadreeBank.Domain.Exceptions;
using KadreeBank.Domain.Interfaces;

namespace KadreeBank.Application.Services;

public class CustomerService(IUnitOfWork unitOfWork) : ICustomerService
{
    public async Task<CustomerDto> CreateCustomerAsync(
        CustomerType type, string fullName, string documentNumber, CancellationToken cancellationToken = default)
    {
        var customer = new Customer
        {
            Type = type,
            FullName = fullName,
            DocumentNumber = documentNumber
        };

        await unitOfWork.Customers.AddAsync(customer, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return customer.ToDto();
    }

    public async Task<CustomerDto> GetByIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var customer = await unitOfWork.Customers.GetByIdAsync(customerId, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), customerId);

        return customer.ToDto();
    }

    public async Task<IReadOnlyList<CustomerDto>> GetAllAsync(
        CustomerType? type, CancellationToken cancellationToken = default)
    {
        var customers = await unitOfWork.Customers.GetAllAsync(type, cancellationToken);

        return customers.Select(c => c.ToDto()).ToList();
    }
}
