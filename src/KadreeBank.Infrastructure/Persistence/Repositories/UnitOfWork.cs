using KadreeBank.Domain.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace KadreeBank.Infrastructure.Persistence.Repositories;

public class UnitOfWork : IUnitOfWork, IAsyncDisposable
{
    private readonly KadreeBankDbContext _dbContext;
    private IDbContextTransaction? _currentTransaction;

    public UnitOfWork(
        KadreeBankDbContext dbContext,
        ICustomerRepository customers,
        IAccountRepository accounts,
        ITransactionRepository transactions)
    {
        _dbContext = dbContext;
        Customers = customers;
        Accounts = accounts;
        Transactions = transactions;
    }

    public ICustomerRepository Customers { get; }
    public IAccountRepository Accounts { get; }
    public ITransactionRepository Transactions { get; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default) =>
        _currentTransaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is null)
            return;

        await _currentTransaction.CommitAsync(cancellationToken);
        await _currentTransaction.DisposeAsync();
        _currentTransaction = null;
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is null)
            return;

        await _currentTransaction.RollbackAsync(cancellationToken);
        await _currentTransaction.DisposeAsync();
        _currentTransaction = null;
    }

    public async ValueTask DisposeAsync()
    {
        if (_currentTransaction is not null)
            await _currentTransaction.DisposeAsync();
    }
}
