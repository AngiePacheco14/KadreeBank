using KadreeBank.Domain.Entities;
using KadreeBank.Domain.Enums;

namespace KadreeBank.Domain.Interfaces;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista todos los clientes, opcionalmente filtrados por tipo. Si <paramref name="type"/>
    /// es null, devuelve todos los clientes sin filtrar.
    /// </summary>
    Task<IReadOnlyList<Customer>> GetAllAsync(CustomerType? type, CancellationToken cancellationToken = default);

    Task AddAsync(Customer customer, CancellationToken cancellationToken = default);
}
