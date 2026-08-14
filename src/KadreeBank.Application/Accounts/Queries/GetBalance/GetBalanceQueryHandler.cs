using KadreeBank.Application.Accounts.Dtos;
using KadreeBank.Application.Services;
using MediatR;

namespace KadreeBank.Application.Accounts.Queries.GetBalance;

public sealed class GetBalanceQueryHandler(IAccountService accountService) : IRequestHandler<GetBalanceQuery, BalanceDto>
{
    public Task<BalanceDto> Handle(GetBalanceQuery request, CancellationToken cancellationToken) =>
        accountService.GetBalanceAsync(request.AccountId, cancellationToken);
}
