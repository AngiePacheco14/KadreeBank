namespace KadreeBank.Domain.Interfaces;

public interface IUnitOfWork
{
    ICustomerRepository Customers { get; }
    IAccountRepository Accounts { get; }
    ITransactionRepository Transactions { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Abre una transacción de base de datos. Debe combinarse con
    /// IAccountRepository.GetForUpdateAsync para operaciones de depósito/retiro,
    /// de modo que el bloqueo de fila viva dentro de esta misma transacción.
    /// </summary>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
