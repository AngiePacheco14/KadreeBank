using KadreeBank.Domain.Enums;

namespace KadreeBank.Domain.Entities;

public class Transaction : BaseEntity
{
    public Guid AccountId { get; set; }
    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public string City { get; set; } = default!;
    public decimal BalanceAfter { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
