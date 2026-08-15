using KadreeBank.Application.Accounts.Dtos;
using KadreeBank.Domain.Enums;

namespace KadreeBank.Application.Services;

public interface IAccountService
{
    Task<AccountDto> CreateAccountAsync(
        Guid customerId, AccountType type, string originCity,
        CancellationToken cancellationToken = default);

    Task<BalanceDto> DepositAsync(
        Guid accountId, decimal amount, string city, CancellationToken cancellationToken = default);

    Task<BalanceDto> WithdrawAsync(
        Guid accountId, decimal amount, string city, CancellationToken cancellationToken = default);

    Task<BalanceDto> GetBalanceAsync(Guid accountId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AccountDto>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TransactionDto>> GetRecentTransactionsAsync(
        Guid accountId, int count, CancellationToken cancellationToken = default);

    Task<MonthlyStatementDto> GetMonthlyStatementAsync(
        Guid accountId, int year, int month, CancellationToken cancellationToken = default);
}
