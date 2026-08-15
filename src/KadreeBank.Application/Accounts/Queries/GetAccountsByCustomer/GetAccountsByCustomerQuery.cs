using KadreeBank.Application.Accounts.Dtos;
using MediatR;

namespace KadreeBank.Application.Accounts.Queries.GetAccountsByCustomer;

public sealed record GetAccountsByCustomerQuery(Guid CustomerId) : IRequest<IReadOnlyList<AccountDto>>;
