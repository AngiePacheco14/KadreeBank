using System.Net.Http.Json;
using FluentAssertions;
using KadreeBank.API.Controllers;
using KadreeBank.Application.Accounts.Dtos;
using KadreeBank.Application.Customers.Dtos;
using KadreeBank.Application.Reports.Dtos;
using KadreeBank.Domain.Enums;
using KadreeBank.IntegrationTests.Common;
using Xunit;

namespace KadreeBank.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public class ReportsTests(CustomWebApplicationFactory factory)
{
    private static async Task<(Guid CustomerId, Guid AccountId)> CreateCustomerWithAccountAsync(
        HttpClient client, string originCity)
    {
        var customerResponse = await client.PostAsJsonAsync(
            "/api/customers",
            new CustomersController.CreateCustomerRequest(CustomerType.Natural, "Cliente Reportes", $"DOC-{Guid.NewGuid():N}"),
            TestJsonOptions.Default);
        customerResponse.EnsureSuccessStatusCode();
        var customer = await customerResponse.Content.ReadFromJsonAsync<CustomerDto>(TestJsonOptions.Default);

        var accountResponse = await client.PostAsJsonAsync(
            "/api/accounts",
            new AccountsController.CreateAccountRequest(customer!.Id, AccountType.Savings, originCity),
            TestJsonOptions.Default);
        accountResponse.EnsureSuccessStatusCode();
        var account = await accountResponse.Content.ReadFromJsonAsync<AccountDto>(TestJsonOptions.Default);

        return (customer.Id, account!.Id);
    }

    [Fact]
    public async Task GetCustomerTransactionCounts_OrdersCustomersByTransactionCountDescending()
    {
        var client = factory.CreateClient();
        var now = DateTime.UtcNow;

        var (activeCustomerId, activeAccountId) = await CreateCustomerWithAccountAsync(client, "Bogotá");
        var (quietCustomerId, quietAccountId) = await CreateCustomerWithAccountAsync(client, "Bogotá");

        // 4 movimientos para el cliente "activo", 1 para el "silencioso".
        for (var i = 0; i < 4; i++)
        {
            var response = await client.PostAsJsonAsync(
                $"/api/accounts/{activeAccountId}/deposits",
                new AccountsController.MovementRequest(10_000m, "Bogotá"));
            response.EnsureSuccessStatusCode();
        }

        var quietResponse = await client.PostAsJsonAsync(
            $"/api/accounts/{quietAccountId}/deposits",
            new AccountsController.MovementRequest(10_000m, "Bogotá"));
        quietResponse.EnsureSuccessStatusCode();

        var report = await client.GetFromJsonAsync<List<CustomerTransactionCountDto>>(
            $"/api/reports/customer-transaction-counts?year={now.Year}&month={now.Month}");

        var relevant = report!
            .Where(r => r.CustomerId == activeCustomerId || r.CustomerId == quietCustomerId)
            .ToList();

        relevant.Should().HaveCount(2);
        relevant.Single(r => r.CustomerId == activeCustomerId).TransactionCount.Should().Be(4);
        relevant.Single(r => r.CustomerId == quietCustomerId).TransactionCount.Should().Be(1);

        var activeIndex = relevant.FindIndex(r => r.CustomerId == activeCustomerId);
        var quietIndex = relevant.FindIndex(r => r.CustomerId == quietCustomerId);
        activeIndex.Should().BeLessThan(quietIndex, "el reporte debe venir ordenado descendentemente por # de transacciones");
    }

    [Fact]
    public async Task GetOutOfCityWithdrawals_OnlyCountsWithdrawalsMadeOutsideOriginCity()
    {
        var client = factory.CreateClient();
        var (customerId, accountId) = await CreateCustomerWithAccountAsync(client, "Bogotá");

        var seedResponse = await client.PostAsJsonAsync(
            $"/api/accounts/{accountId}/deposits",
            new AccountsController.MovementRequest(5_000_000m, "Bogotá"));
        seedResponse.EnsureSuccessStatusCode();

        // Retiro dentro de la ciudad de origen: no debe contar para el reporte.
        var localWithdrawal = await client.PostAsJsonAsync(
            $"/api/accounts/{accountId}/withdrawals",
            new AccountsController.MovementRequest(500_000m, "Bogotá"));
        localWithdrawal.EnsureSuccessStatusCode();

        // Retiros fuera de la ciudad de origen: 700.000 + 600.000 = 1.300.000 > umbral.
        var outOfCityWithdrawal1 = await client.PostAsJsonAsync(
            $"/api/accounts/{accountId}/withdrawals",
            new AccountsController.MovementRequest(700_000m, "Cali"));
        outOfCityWithdrawal1.EnsureSuccessStatusCode();

        var outOfCityWithdrawal2 = await client.PostAsJsonAsync(
            $"/api/accounts/{accountId}/withdrawals",
            new AccountsController.MovementRequest(600_000m, "Cali"));
        outOfCityWithdrawal2.EnsureSuccessStatusCode();

        var response = await client.GetAsync("/api/reports/out-of-city-withdrawals?minAmount=1000000");
        var body = await response.Content.ReadAsStringAsync();
        response.IsSuccessStatusCode.Should().BeTrue(body);

        var report = System.Text.Json.JsonSerializer.Deserialize<List<OutOfCityWithdrawalDto>>(
            body, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));

        var entry = report!.SingleOrDefault(r => r.CustomerId == customerId);

        entry.Should().NotBeNull();
        entry!.TotalWithdrawn.Should().Be(1_300_000m);
    }
}
