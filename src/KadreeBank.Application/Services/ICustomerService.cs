using KadreeBank.Application.Customers.Dtos;
using KadreeBank.Domain.Enums;

namespace KadreeBank.Application.Services;

public interface ICustomerService
{
    Task<CustomerDto> CreateCustomerAsync(
        CustomerType type, string fullName, string documentNumber, CancellationToken cancellationToken = default);

    Task<CustomerDto> GetByIdAsync(Guid customerId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CustomerDto>> GetAllAsync(CustomerType? type, CancellationToken cancellationToken = default);
}
