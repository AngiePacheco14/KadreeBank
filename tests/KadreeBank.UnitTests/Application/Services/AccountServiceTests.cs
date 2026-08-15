using FluentAssertions;
using KadreeBank.Application.Services;
using KadreeBank.Domain.Entities;
using KadreeBank.Domain.Enums;
using KadreeBank.Domain.Exceptions;
using KadreeBank.Domain.Interfaces;
using Moq;
using Xunit;

namespace KadreeBank.UnitTests.Application.Services;

public class AccountServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICustomerRepository> _customerRepository = new();
    private readonly Mock<IAccountRepository> _accountRepository = new();
    private readonly Mock<ITransactionRepository> _transactionRepository = new();
    private readonly AccountService _sut;

    public AccountServiceTests()
    {
        _unitOfWork.SetupGet(u => u.Customers).Returns(_customerRepository.Object);
        _unitOfWork.SetupGet(u => u.Accounts).Returns(_accountRepository.Object);
        _unitOfWork.SetupGet(u => u.Transactions).Returns(_transactionRepository.Object);
        _sut = new AccountService(_unitOfWork.Object);
    }

    private static Customer NaturalCustomer() => new()
    {
        Type = CustomerType.Natural,
        FullName = "Ana Pérez",
        DocumentNumber = "CC-123"
    };

    private static Customer BusinessCustomer() => new()
    {
        Type = CustomerType.Business,
        FullName = "Acme S.A.S",
        DocumentNumber = "NIT-999"
    };

    // ---------- CreateAccountAsync ----------

    [Fact]
    public async Task CreateAccountAsync_NaturalCustomerRequestingSavings_Succeeds()
    {
        var customer = NaturalCustomer();
        _customerRepository.Setup(r => r.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>())).ReturnsAsync(customer);

        var result = await _sut.CreateAccountAsync(customer.Id, AccountType.Savings, "Bogotá");

        result.Type.Should().Be(AccountType.Savings);
        result.Balance.Should().Be(0m);
        result.AccountNumber.Should().StartWith("SAV-");
        _accountRepository.Verify(r => r.AddAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAccountAsync_NaturalCustomerRequestingChecking_ThrowsInvalidAccountType()
    {
        var customer = NaturalCustomer();
        _customerRepository.Setup(r => r.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>())).ReturnsAsync(customer);

        var act = () => _sut.CreateAccountAsync(customer.Id, AccountType.Checking, "Bogotá");

        await act.Should().ThrowAsync<InvalidAccountTypeException>();
    }

    [Fact]
    public async Task CreateAccountAsync_BusinessCustomerRequestingSavings_ThrowsInvalidAccountType()
    {
        var customer = BusinessCustomer();
        _customerRepository.Setup(r => r.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>())).ReturnsAsync(customer);

        var act = () => _sut.CreateAccountAsync(customer.Id, AccountType.Savings, "Bogotá");

        await act.Should().ThrowAsync<InvalidAccountTypeException>();
    }

    [Fact]
    public async Task CreateAccountAsync_WhenCustomerDoesNotExist_ThrowsNotFound()
    {
        _customerRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        var act = () => _sut.CreateAccountAsync(Guid.NewGuid(), AccountType.Savings, "Bogotá");

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ---------- DepositAsync ----------

    [Fact]
    public async Task DepositAsync_WithExistingAccount_LocksAccountDepositsAndCommits()
    {
        var account = new Account { Type = AccountType.Savings, AccountNumber = "SAV-001", OriginCity = "Bogotá", Balance = 0m };
        _accountRepository.Setup(r => r.GetForUpdateAsync(account.Id, It.IsAny<CancellationToken>())).ReturnsAsync(account);

        var result = await _sut.DepositAsync(account.Id, 200_000m, "Bogotá");

        result.Balance.Should().Be(200_000m);
        _unitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _transactionRepository.Verify(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public async Task DepositAsync_WithNonPositiveAmount_ThrowsInvalidTransactionAmount(decimal amount)
    {
        var act = () => _sut.DepositAsync(Guid.NewGuid(), amount, "Bogotá");

        await act.Should().ThrowAsync<InvalidTransactionAmountException>();
    }

    [Fact]
    public async Task DepositAsync_WhenAccountDoesNotExist_RollsBackAndThrowsNotFound()
    {
        _accountRepository.Setup(r => r.GetForUpdateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);

        var act = () => _sut.DepositAsync(Guid.NewGuid(), 100m, "Bogotá");

        await act.Should().ThrowAsync<NotFoundException>();
        _unitOfWork.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---------- WithdrawAsync ----------

    [Fact]
    public async Task WithdrawAsync_WithSufficientBalance_WithdrawsAndCommits()
    {
        var account = new Account { Type = AccountType.Savings, AccountNumber = "SAV-001", OriginCity = "Bogotá", Balance = 1_000_000m };
        _accountRepository.Setup(r => r.GetForUpdateAsync(account.Id, It.IsAny<CancellationToken>())).ReturnsAsync(account);

        var result = await _sut.WithdrawAsync(account.Id, 300_000m, "Bogotá");

        result.Balance.Should().Be(700_000m);
        _unitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task WithdrawAsync_ExceedingBalance_RollsBackAndThrowsInsufficientFunds()
    {
        var account = new Account { Type = AccountType.Savings, AccountNumber = "SAV-001", OriginCity = "Bogotá", Balance = 100_000m };
        _accountRepository.Setup(r => r.GetForUpdateAsync(account.Id, It.IsAny<CancellationToken>())).ReturnsAsync(account);

        var act = () => _sut.WithdrawAsync(account.Id, 200_000m, "Bogotá");

        await act.Should().ThrowAsync<InsufficientFundsException>();
        _unitOfWork.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _transactionRepository.Verify(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Never);
        account.Balance.Should().Be(100_000m, "un intento fallido de retiro no debe alterar el saldo");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-50)]
    public async Task WithdrawAsync_WithNonPositiveAmount_ThrowsInvalidTransactionAmount(decimal amount)
    {
        var act = () => _sut.WithdrawAsync(Guid.NewGuid(), amount, "Bogotá");

        await act.Should().ThrowAsync<InvalidTransactionAmountException>();
    }

    // ---------- GetBalanceAsync ----------

    [Fact]
    public async Task GetBalanceAsync_WhenAccountDoesNotExist_ThrowsNotFound()
    {
        _accountRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);

        var act = () => _sut.GetBalanceAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ---------- GetByCustomerIdAsync ----------

    [Fact]
    public async Task GetByCustomerIdAsync_WithExistingCustomer_ReturnsTheirAccounts()
    {
        var customer = NaturalCustomer();
        var account = new Account
        {
            CustomerId = customer.Id, Type = AccountType.Savings, AccountNumber = "SAV-001", OriginCity = "Bogotá"
        };
        _customerRepository.Setup(r => r.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>())).ReturnsAsync(customer);
        _accountRepository.Setup(r => r.GetByCustomerIdAsync(customer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([account]);

        var result = await _sut.GetByCustomerIdAsync(customer.Id);

        result.Should().ContainSingle(a => a.Id == account.Id);
    }

    [Fact]
    public async Task GetByCustomerIdAsync_WhenCustomerDoesNotExist_ThrowsNotFound()
    {
        _customerRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        var act = () => _sut.GetByCustomerIdAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
