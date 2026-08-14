using KadreeBank.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KadreeBank.Infrastructure.Persistence;

public class KadreeBankDbContext(DbContextOptions<KadreeBankDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(KadreeBankDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
