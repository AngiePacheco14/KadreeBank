using KadreeBank.Domain.Enums;

namespace KadreeBank.Domain.Entities;

public class Account : BaseEntity
{
    public Guid CustomerId { get; set; }
    public string AccountNumber { get; set; } = default!;
    public AccountType Type { get; set; }
    public decimal Balance { get; set; }
    public string OriginCity { get; set; } = default!;
}
