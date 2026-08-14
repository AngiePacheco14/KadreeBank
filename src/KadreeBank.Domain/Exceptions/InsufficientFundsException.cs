namespace KadreeBank.Domain.Exceptions;

public sealed class InsufficientFundsException : DomainException
{
    public InsufficientFundsException(Guid accountId, decimal balance, decimal requestedAmount)
        : base($"La cuenta {accountId} tiene saldo ${balance:N2} y no puede cubrir un retiro de ${requestedAmount:N2}.")
    {
    }
}
