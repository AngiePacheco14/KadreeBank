using KadreeBank.Application.Accounts.Dtos;
using KadreeBank.Application.Accounts.Queries.GetAccountsByCustomer;
using KadreeBank.Application.Customers.Commands.CreateCustomer;
using KadreeBank.Application.Customers.Dtos;
using KadreeBank.Application.Customers.Queries.GetCustomerById;
using KadreeBank.Application.Customers.Queries.GetCustomers;
using KadreeBank.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace KadreeBank.API.Controllers;

[ApiController]
[Route("api/customers")]
public class CustomersController(ISender sender) : ControllerBase
{
    public sealed record CreateCustomerRequest(CustomerType Type, string FullName, string DocumentNumber);

    [HttpPost]
    [ProducesResponseType<CustomerDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<CustomerDto>> Create(
        [FromBody] CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateCustomerCommand(request.Type, request.FullName, request.DocumentNumber);
        var customer = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = customer.Id }, customer);
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CustomerDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CustomerDto>>> GetAll(
        [FromQuery] CustomerType? type, CancellationToken cancellationToken)
    {
        var customers = await sender.Send(new GetCustomersQuery(type), cancellationToken);
        return Ok(customers);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<CustomerDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CustomerDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var customer = await sender.Send(new GetCustomerByIdQuery(id), cancellationToken);
        return Ok(customer);
    }

    [HttpGet("{id:guid}/accounts")]
    [ProducesResponseType<IReadOnlyList<AccountDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AccountDto>>> GetAccounts(Guid id, CancellationToken cancellationToken)
    {
        var accounts = await sender.Send(new GetAccountsByCustomerQuery(id), cancellationToken);
        return Ok(accounts);
    }
}
