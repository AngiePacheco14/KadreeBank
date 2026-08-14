using KadreeBank.Application.Accounts.Dtos;
using KadreeBank.Application.Common.Mappings;
using KadreeBank.Domain.Entities;
using KadreeBank.Domain.Enums;
using KadreeBank.Domain.Exceptions;
using KadreeBank.Domain.Interfaces;

namespace KadreeBank.Application.Services;

public class AccountService(IUnitOfWork unitOfWork) : IAccountService
{
    public async Task<AccountDto> CreateAccountAsync(
        Guid customerId, AccountType type, string originCity,
        CancellationToken cancellationToken = default)
    {
        var customer = await unitOfWork.Customers.GetByIdAsync(customerId, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), customerId);

        // Un cliente natural solo puede abrir cuentas de ahorro, y una empresa solo
        // cuentas corrientes.
        var expectedType = customer.Type == CustomerType.Natural ? AccountType.Savings : AccountType.Checking;
        if (type != expectedType)
            throw new InvalidAccountTypeException(customer.Type, type);

        var account = new Account
        {
            CustomerId = customer.Id,
            Type = type,
            OriginCity = originCity,
            Balance = 0m
        };
        // Derivado del Id (ya único por Guid.NewGuid() en BaseEntity), así que no
        // requiere ida y vuelta a la base de datos ni lógica de reintento ante colisiones.
        account.AccountNumber = GenerateAccountNumber(type, account.Id);

        await unitOfWork.Accounts.AddAsync(account, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return account.ToDto();
    }

    private static string GenerateAccountNumber(AccountType type, Guid id)
    {
        var prefix = type == AccountType.Savings ? "SAV" : "CHK";
        return $"{prefix}-{id:N}".ToUpperInvariant();
    }

    public async Task<BalanceDto> DepositAsync(
        Guid accountId, decimal amount, string city, CancellationToken cancellationToken = default)
    {
        if (amount <= 0)
            throw new InvalidTransactionAmountException(amount);

        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // GetForUpdateAsync bloquea la fila de la cuenta (SELECT ... FOR UPDATE) mientras
            // dure esta transacción, serializando cualquier depósito/retiro concurrente sobre
            // la misma cuenta y evitando lecturas de saldo obsoletas (lost update).
            var account = await unitOfWork.Accounts.GetForUpdateAsync(accountId, cancellationToken)
                ?? throw new NotFoundException(nameof(Account), accountId);

            account.Balance += amount;

            var transaction = new Transaction
            {
                AccountId = account.Id,
                Type = TransactionType.Deposit,
                Amount = amount,
                City = city,
                BalanceAfter = account.Balance
            };

            await unitOfWork.Transactions.AddAsync(transaction, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);

            return new BalanceDto(account.Id, account.Balance);
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task<BalanceDto> WithdrawAsync(
        Guid accountId, decimal amount, string city, CancellationToken cancellationToken = default)
    {
        if (amount <= 0)
            throw new InvalidTransactionAmountException(amount);

        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var account = await unitOfWork.Accounts.GetForUpdateAsync(accountId, cancellationToken)
                ?? throw new NotFoundException(nameof(Account), accountId);

            // El saldo de una cuenta nunca puede quedar negativo.
            if (account.Balance - amount < 0)
                throw new InsufficientFundsException(account.Id, account.Balance, amount);

            account.Balance -= amount;

            var transaction = new Transaction
            {
                AccountId = account.Id,
                Type = TransactionType.Withdrawal,
                Amount = amount,
                City = city,
                BalanceAfter = account.Balance
            };

            await unitOfWork.Transactions.AddAsync(transaction, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);

            return new BalanceDto(account.Id, account.Balance);
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task<BalanceDto> GetBalanceAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        var account = await unitOfWork.Accounts.GetByIdAsync(accountId, cancellationToken)
            ?? throw new NotFoundException(nameof(Account), accountId);

        return new BalanceDto(account.Id, account.Balance);
    }

    public async Task<IReadOnlyList<TransactionDto>> GetRecentTransactionsAsync(
        Guid accountId, int count, CancellationToken cancellationToken = default)
    {
        _ = await unitOfWork.Accounts.GetByIdAsync(accountId, cancellationToken)
            ?? throw new NotFoundException(nameof(Account), accountId);

        var transactions = await unitOfWork.Transactions.GetRecentByAccountIdAsync(accountId, count, cancellationToken);

        return transactions.Select(t => t.ToDto()).ToList();
    }

    public async Task<MonthlyStatementDto> GetMonthlyStatementAsync(
        Guid accountId, int year, int month, CancellationToken cancellationToken = default)
    {
        var account = await unitOfWork.Accounts.GetByIdAsync(accountId, cancellationToken)
            ?? throw new NotFoundException(nameof(Account), accountId);

        var fromUtc = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var toUtc = fromUtc.AddMonths(1);

        var movements = await unitOfWork.Transactions.GetByAccountIdAndDateRangeAsync(
            accountId, fromUtc, toUtc, cancellationToken);

        var ordered = movements.OrderBy(t => t.Timestamp).ToList();

        // Sin movimientos en el mes: el saldo no cambió durante ese período, así que
        // apertura y cierre coinciden con el saldo actual de la cuenta.
        var openingBalance = account.Balance;
        var closingBalance = account.Balance;

        if (ordered.Count > 0)
        {
            var first = ordered[0];
            openingBalance = first.Type == TransactionType.Deposit
                ? first.BalanceAfter - first.Amount
                : first.BalanceAfter + first.Amount;

            closingBalance = ordered[^1].BalanceAfter;
        }

        return new MonthlyStatementDto(
            account.Id,
            year,
            month,
            openingBalance,
            closingBalance,
            ordered.Select(t => t.ToDto()).ToList());
    }
}
