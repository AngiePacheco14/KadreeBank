using KadreeBank.Domain.Entities;

namespace KadreeBank.Domain.Interfaces;

public interface ITransactionRepository
{
    Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Transaction>> GetRecentByAccountIdAsync(
        Guid accountId, int count, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Transaction>> GetByAccountIdAndDateRangeAsync(
        Guid accountId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);
}
