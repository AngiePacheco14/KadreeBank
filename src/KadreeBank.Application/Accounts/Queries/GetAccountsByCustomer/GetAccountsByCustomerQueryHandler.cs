using KadreeBank.Application.Accounts.Dtos;
using KadreeBank.Application.Services;
using MediatR;

namespace KadreeBank.Application.Accounts.Queries.GetAccountsByCustomer;

public sealed class GetAccountsByCustomerQueryHandler(IAccountService accountService)
    : IRequestHandler<GetAccountsByCustomerQuery, IReadOnlyList<AccountDto>>
{
    public Task<IReadOnlyList<AccountDto>> Handle(
        GetAccountsByCustomerQuery request, CancellationToken cancellationToken) =>
        accountService.GetByCustomerIdAsync(request.CustomerId, cancellationToken);
}
