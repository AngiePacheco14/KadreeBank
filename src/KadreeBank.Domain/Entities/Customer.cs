using KadreeBank.Domain.Enums;

namespace KadreeBank.Domain.Entities;

public class Customer : BaseEntity
{
    public CustomerType Type { get; set; }
    public string FullName { get; set; } = default!;
    public string DocumentNumber { get; set; } = default!;
}
