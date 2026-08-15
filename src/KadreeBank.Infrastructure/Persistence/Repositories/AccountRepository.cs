using KadreeBank.Domain.Entities;
using KadreeBank.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KadreeBank.Infrastructure.Persistence.Repositories;

public class AccountRepository(KadreeBankDbContext dbContext) : IAccountRepository
{
    public Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Accounts.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public Task<Account?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Accounts
            .FromSqlInterpolated($"""SELECT * FROM accounts WHERE "Id" = {id} FOR UPDATE""")
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<Account>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default) =>
        await dbContext.Accounts
            .Where(a => a.CustomerId == customerId)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Account account, CancellationToken cancellationToken = default) =>
        await dbContext.Accounts.AddAsync(account, cancellationToken);
}
