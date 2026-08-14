using KadreeBank.Application.Reports.Dtos;
using KadreeBank.Application.Reports.Queries.GetCustomerTransactionCounts;
using KadreeBank.Application.Reports.Queries.GetOutOfCityWithdrawals;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace KadreeBank.API.Controllers;

[ApiController]
[Route("api/reports")]
public class ReportsController(ISender sender) : ControllerBase
{
    [HttpGet("customer-transaction-counts")]
    [ProducesResponseType<IReadOnlyList<CustomerTransactionCountDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CustomerTransactionCountDto>>> GetCustomerTransactionCounts(
        [FromQuery] int year, [FromQuery] int month, CancellationToken cancellationToken)
    {
        var report = await sender.Send(new GetCustomerTransactionCountsQuery(year, month), cancellationToken);
        return Ok(report);
    }

    [HttpGet("out-of-city-withdrawals")]
    [ProducesResponseType<IReadOnlyList<OutOfCityWithdrawalDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<OutOfCityWithdrawalDto>>> GetOutOfCityWithdrawals(
        [FromQuery] decimal minAmount = 1_000_000m, CancellationToken cancellationToken = default)
    {
        var report = await sender.Send(new GetOutOfCityWithdrawalsQuery(minAmount), cancellationToken);
        return Ok(report);
    }
}
