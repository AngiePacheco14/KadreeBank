using KadreeBank.Application.Accounts.Commands.CreateAccount;
using KadreeBank.Application.Accounts.Commands.Deposit;
using KadreeBank.Application.Accounts.Commands.Withdraw;
using KadreeBank.Application.Accounts.Dtos;
using KadreeBank.Application.Accounts.Queries.GetBalance;
using KadreeBank.Application.Accounts.Queries.GetMonthlyStatement;
using KadreeBank.Application.Accounts.Queries.GetRecentTransactions;
using KadreeBank.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace KadreeBank.API.Controllers;

[ApiController]
[Route("api/accounts")]
public class AccountsController(ISender sender) : ControllerBase
{
    public sealed record CreateAccountRequest(Guid CustomerId, AccountType Type, string OriginCity);
    public sealed record MovementRequest(decimal Amount, string City);

    [HttpPost]
    [ProducesResponseType<AccountDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<AccountDto>> Create(
        [FromBody] CreateAccountRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateAccountCommand(request.CustomerId, request.Type, request.OriginCity);
        var account = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetBalance), new { id = account.Id }, account);
    }

    [HttpPost("{id:guid}/deposits")]
    [ProducesResponseType<BalanceDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BalanceDto>> Deposit(
        Guid id, [FromBody] MovementRequest request, CancellationToken cancellationToken)
    {
        var balance = await sender.Send(new DepositCommand(id, request.Amount, request.City), cancellationToken);
        return Ok(balance);
    }

    [HttpPost("{id:guid}/withdrawals")]
    [ProducesResponseType<BalanceDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BalanceDto>> Withdraw(
        Guid id, [FromBody] MovementRequest request, CancellationToken cancellationToken)
    {
        var balance = await sender.Send(new WithdrawCommand(id, request.Amount, request.City), cancellationToken);
        return Ok(balance);
    }

    [HttpGet("{id:guid}/balance")]
    [ProducesResponseType<BalanceDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BalanceDto>> GetBalance(Guid id, CancellationToken cancellationToken)
    {
        var balance = await sender.Send(new GetBalanceQuery(id), cancellationToken);
        return Ok(balance);
    }

    [HttpGet("{id:guid}/transactions/recent")]
    [ProducesResponseType<IReadOnlyList<TransactionDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TransactionDto>>> GetRecentTransactions(
        Guid id, [FromQuery] int count = 10, CancellationToken cancellationToken = default)
    {
        var transactions = await sender.Send(new GetRecentTransactionsQuery(id, count), cancellationToken);
        return Ok(transactions);
    }

    [HttpGet("{id:guid}/statements/{year:int}/{month:int}")]
    [ProducesResponseType<MonthlyStatementDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<MonthlyStatementDto>> GetMonthlyStatement(
        Guid id, int year, int month, CancellationToken cancellationToken)
    {
        var statement = await sender.Send(new GetMonthlyStatementQuery(id, year, month), cancellationToken);
        return Ok(statement);
    }
}
