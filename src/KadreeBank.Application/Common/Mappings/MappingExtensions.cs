using KadreeBank.Application.Accounts.Dtos;
using KadreeBank.Application.Customers.Dtos;
using KadreeBank.Domain.Entities;

namespace KadreeBank.Application.Common.Mappings;

public static class MappingExtensions
{
    public static CustomerDto ToDto(this Customer customer) =>
        new(customer.Id, customer.FullName, customer.DocumentNumber, customer.Type, customer.CreatedAt);

    public static AccountDto ToDto(this Account account) =>
        new(account.Id, account.CustomerId, account.AccountNumber, account.Type, account.Balance, account.OriginCity, account.CreatedAt);

    public static TransactionDto ToDto(this Transaction transaction) =>
        new(transaction.Id, transaction.AccountId, transaction.Type, transaction.Amount, transaction.City, transaction.BalanceAfter, transaction.Timestamp);
}
