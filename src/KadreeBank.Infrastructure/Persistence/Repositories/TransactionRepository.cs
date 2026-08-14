using KadreeBank.Domain.Entities;
using KadreeBank.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KadreeBank.Infrastructure.Persistence.Repositories;

public class TransactionRepository(KadreeBankDbContext dbContext) : ITransactionRepository
{
    public async Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default) =>
        await dbContext.Transactions.AddAsync(transaction, cancellationToken);

    public async Task<IReadOnlyList<Transaction>> GetRecentByAccountIdAsync(
        Guid accountId, int count, CancellationToken cancellationToken = default) =>
        await dbContext.Transactions
            .Where(t => t.AccountId == accountId)
            .OrderByDescending(t => t.Timestamp)
            .Take(count)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Transaction>> GetByAccountIdAndDateRangeAsync(
        Guid accountId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default) =>
        await dbContext.Transactions
            .Where(t => t.AccountId == accountId && t.Timestamp >= fromUtc && t.Timestamp < toUtc)
            .OrderBy(t => t.Timestamp)
            .ToListAsync(cancellationToken);
}
