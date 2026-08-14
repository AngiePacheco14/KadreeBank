using FluentAssertions;
using KadreeBank.Application.Reports.Dtos;
using KadreeBank.Application.Reports.Interfaces;
using KadreeBank.Application.Services;
using Moq;
using Xunit;

namespace KadreeBank.UnitTests.Application.Services;

public class ReportServiceTests
{
    [Fact]
    public async Task GetCustomerTransactionCountsAsync_DelegatesToReportQueriesWithRequestedPeriod()
    {
        var expected = new List<CustomerTransactionCountDto>
        {
            new(Guid.NewGuid(), "Ana Pérez", 5),
            new(Guid.NewGuid(), "Acme S.A.S", 2)
        };

        var reportQueries = new Mock<IReportQueries>();
        reportQueries.Setup(r => r.GetCustomerTransactionCountsAsync(2026, 8, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var sut = new ReportService(reportQueries.Object);

        var result = await sut.GetCustomerTransactionCountsAsync(2026, 8);

        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetOutOfCityWithdrawalsAsync_DelegatesToReportQueriesWithRequestedMinimum()
    {
        var expected = new List<OutOfCityWithdrawalDto>
        {
            new(Guid.NewGuid(), "Ana Pérez", 1_500_000m)
        };

        var reportQueries = new Mock<IReportQueries>();
        reportQueries.Setup(r => r.GetOutOfCityWithdrawalsAsync(1_000_000m, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var sut = new ReportService(reportQueries.Object);

        var result = await sut.GetOutOfCityWithdrawalsAsync(1_000_000m);

        result.Should().BeEquivalentTo(expected);
    }
}
