using KadreeBank.Domain.Entities;

namespace KadreeBank.Domain.Interfaces;

public interface IAccountRepository
{
    Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Carga la cuenta bloqueando la fila a nivel de base de datos (SELECT ... FOR UPDATE).
    /// Debe usarse siempre dentro de una transacción abierta por IUnitOfWork antes de
    /// mutar el saldo, para serializar operaciones concurrentes sobre la misma cuenta.
    /// </summary>
    Task<Account?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Account>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);

    Task AddAsync(Account account, CancellationToken cancellationToken = default);
}
