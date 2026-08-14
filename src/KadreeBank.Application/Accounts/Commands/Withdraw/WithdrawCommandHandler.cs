using KadreeBank.Application.Accounts.Dtos;
using KadreeBank.Application.Services;
using MediatR;

namespace KadreeBank.Application.Accounts.Commands.Withdraw;

public sealed class WithdrawCommandHandler(IAccountService accountService) : IRequestHandler<WithdrawCommand, BalanceDto>
{
    public Task<BalanceDto> Handle(WithdrawCommand request, CancellationToken cancellationToken) =>
        accountService.WithdrawAsync(request.AccountId, request.Amount, request.City, cancellationToken);
}
