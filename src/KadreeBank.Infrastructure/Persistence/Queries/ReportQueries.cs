using KadreeBank.Application.Reports.Dtos;
using KadreeBank.Application.Reports.Interfaces;
using KadreeBank.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace KadreeBank.Infrastructure.Persistence.Queries;

public class ReportQueries(KadreeBankDbContext dbContext) : IReportQueries
{
    public async Task<IReadOnlyList<CustomerTransactionCountDto>> GetCustomerTransactionCountsAsync(
        int year, int month, CancellationToken cancellationToken = default)
    {
        var fromUtc = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var toUtc = fromUtc.AddMonths(1);

        var query =
            from t in dbContext.Transactions
            where t.Timestamp >= fromUtc && t.Timestamp < toUtc
            join a in dbContext.Accounts on t.AccountId equals a.Id
            join c in dbContext.Customers on a.CustomerId equals c.Id
            group c by new { c.Id, c.FullName } into g
            orderby g.Count() descending
            select new CustomerTransactionCountDto(g.Key.Id, g.Key.FullName, g.Count());

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OutOfCityWithdrawalDto>> GetOutOfCityWithdrawalsAsync(
        decimal minAmount, CancellationToken cancellationToken = default)
    {
        // El resultado ya viene agregado por cliente (a lo sumo un puñado de filas), así
        // que se agrupa/suma en el servidor y el filtro por monto mínimo + orden se aplican
        // en memoria: EF Core no logra traducir el HAVING cuando se proyecta directamente
        // al record del DTO dentro del mismo Select que compone la agregación.
        var aggregated = await (
            from t in dbContext.Transactions
            where t.Type == TransactionType.Withdrawal
            join a in dbContext.Accounts on t.AccountId equals a.Id
            where t.City != a.OriginCity
            join c in dbContext.Customers on a.CustomerId equals c.Id
            group t by new { c.Id, c.FullName } into g
            select new { g.Key.Id, g.Key.FullName, Total = g.Sum(x => x.Amount) })
            .ToListAsync(cancellationToken);

        return aggregated
            .Where(r => r.Total > minAmount)
            .OrderByDescending(r => r.Total)
            .Select(r => new OutOfCityWithdrawalDto(r.Id, r.FullName, r.Total))
            .ToList();
    }
}
