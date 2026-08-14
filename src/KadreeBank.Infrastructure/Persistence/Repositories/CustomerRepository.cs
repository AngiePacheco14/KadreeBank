using KadreeBank.Domain.Entities;
using KadreeBank.Domain.Enums;
using KadreeBank.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KadreeBank.Infrastructure.Persistence.Repositories;

public class CustomerRepository(KadreeBankDbContext dbContext) : ICustomerRepository
{
    public Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Customers.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Customer>> GetAllAsync(
        CustomerType? type, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Customers.AsQueryable();

        if (type is not null)
            query = query.Where(c => c.Type == type);

        return await query.ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Customer customer, CancellationToken cancellationToken = default) =>
        await dbContext.Customers.AddAsync(customer, cancellationToken);
}
