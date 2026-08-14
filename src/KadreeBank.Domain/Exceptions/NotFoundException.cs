namespace KadreeBank.Domain.Exceptions;

public sealed class NotFoundException : DomainException
{
    public NotFoundException(string entityName, Guid id)
        : base($"{entityName} con id '{id}' no fue encontrado.")
    {
    }
}
