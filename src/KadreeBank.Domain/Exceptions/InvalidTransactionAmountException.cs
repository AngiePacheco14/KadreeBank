namespace KadreeBank.Domain.Exceptions;

public sealed class InvalidTransactionAmountException : DomainException
{
    public InvalidTransactionAmountException(decimal amount)
        : base($"El monto ${amount:N2} no es válido. Debe ser mayor que cero.")
    {
    }
}
