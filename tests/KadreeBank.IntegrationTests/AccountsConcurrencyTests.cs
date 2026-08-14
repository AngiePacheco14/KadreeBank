using System.Net.Http.Json;
using FluentAssertions;
using KadreeBank.API.Controllers;
using KadreeBank.Application.Accounts.Dtos;
using KadreeBank.Application.Customers.Dtos;
using KadreeBank.Domain.Enums;
using KadreeBank.IntegrationTests.Common;
using Xunit;

namespace KadreeBank.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public class AccountsConcurrencyTests(CustomWebApplicationFactory factory)
{
    private async Task<Guid> CreateAccountAsync(HttpClient client, string originCity)
    {
        var customerResponse = await client.PostAsJsonAsync(
            "/api/customers",
            new CustomersController.CreateCustomerRequest(CustomerType.Natural, "Cliente de prueba", $"DOC-{Guid.NewGuid():N}"),
            TestJsonOptions.Default);
        customerResponse.EnsureSuccessStatusCode();
        var customer = await customerResponse.Content.ReadFromJsonAsync<CustomerDto>(TestJsonOptions.Default);

        var accountResponse = await client.PostAsJsonAsync(
            "/api/accounts",
            new AccountsController.CreateAccountRequest(customer!.Id, AccountType.Savings, originCity),
            TestJsonOptions.Default);
        accountResponse.EnsureSuccessStatusCode();
        var account = await accountResponse.Content.ReadFromJsonAsync<AccountDto>(TestJsonOptions.Default);

        return account!.Id;
    }

    /// <summary>
    /// Dispara depósitos y retiros en paralelo contra la misma cuenta y verifica que el
    /// saldo final es exactamente el esperado. Si el bloqueo por fila (SELECT ... FOR
    /// UPDATE) no estuviera funcionando, dos operaciones concurrentes podrían leer el
    /// mismo saldo inicial y perder una actualización (lost update), dejando un saldo
    /// final incorrecto.
    /// </summary>
    [Fact]
    public async Task ConcurrentDepositsAndWithdrawals_KeepBalanceConsistent()
    {
        var client = factory.CreateClient();
        var accountId = await CreateAccountAsync(client, "Bogotá");

        var seedResponse = await client.PostAsJsonAsync(
            $"/api/accounts/{accountId}/deposits",
            new AccountsController.MovementRequest(1_000_000m, "Bogotá"));
        seedResponse.EnsureSuccessStatusCode();

        const int depositCount = 20;
        const decimal depositAmount = 50_000m;
        const int withdrawalCount = 10;
        const decimal withdrawalAmount = 20_000m;

        var depositTasks = Enumerable.Range(0, depositCount).Select(_ =>
            client.PostAsJsonAsync(
                $"/api/accounts/{accountId}/deposits",
                new AccountsController.MovementRequest(depositAmount, "Bogotá")));

        var withdrawalTasks = Enumerable.Range(0, withdrawalCount).Select(_ =>
            client.PostAsJsonAsync(
                $"/api/accounts/{accountId}/withdrawals",
                new AccountsController.MovementRequest(withdrawalAmount, "Bogotá")));

        var responses = await Task.WhenAll(depositTasks.Concat(withdrawalTasks));

        responses.Should().OnlyContain(r => r.IsSuccessStatusCode);

        var balanceResponse = await client.GetFromJsonAsync<BalanceDto>($"/api/accounts/{accountId}/balance");

        var expectedBalance = 1_000_000m
            + (depositCount * depositAmount)
            - (withdrawalCount * withdrawalAmount);

        balanceResponse!.Balance.Should().Be(expectedBalance);
    }

    [Fact]
    public async Task Withdraw_NeverDrivesBalanceNegativeUnderConcurrentAttempts()
    {
        var client = factory.CreateClient();
        var accountId = await CreateAccountAsync(client, "Bogotá");

        var seedResponse = await client.PostAsJsonAsync(
            $"/api/accounts/{accountId}/deposits",
            new AccountsController.MovementRequest(100_000m, "Bogotá"));
        seedResponse.EnsureSuccessStatusCode();

        // 5 retiros concurrentes de 30.000 sobre un saldo de 100.000: solo 3 pueden
        // completarse (90.000); los otros 2 deben fallar de forma controlada.
        var withdrawalTasks = Enumerable.Range(0, 5).Select(_ =>
            client.PostAsJsonAsync(
                $"/api/accounts/{accountId}/withdrawals",
                new AccountsController.MovementRequest(30_000m, "Bogotá")));

        var responses = await Task.WhenAll(withdrawalTasks);

        responses.Count(r => r.IsSuccessStatusCode).Should().Be(3);

        var balanceResponse = await client.GetFromJsonAsync<BalanceDto>($"/api/accounts/{accountId}/balance");
        balanceResponse!.Balance.Should().Be(10_000m);
        balanceResponse.Balance.Should().BeGreaterThanOrEqualTo(0m);
    }
}
