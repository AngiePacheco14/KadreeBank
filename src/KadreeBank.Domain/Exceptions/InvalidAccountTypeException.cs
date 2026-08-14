using KadreeBank.Domain.Enums;

namespace KadreeBank.Domain.Exceptions;

public sealed class InvalidAccountTypeException : DomainException
{
    public InvalidAccountTypeException(CustomerType customerType, AccountType accountType)
        : base($"Un cliente de tipo {customerType} no puede tener una cuenta de tipo {accountType}.")
    {
    }
}
