using KadreeBank.Application.Accounts.Dtos;
using KadreeBank.Application.Services;
using MediatR;

namespace KadreeBank.Application.Accounts.Commands.Deposit;

public sealed class DepositCommandHandler(IAccountService accountService) : IRequestHandler<DepositCommand, BalanceDto>
{
    public Task<BalanceDto> Handle(DepositCommand request, CancellationToken cancellationToken) =>
        accountService.DepositAsync(request.AccountId, request.Amount, request.City, cancellationToken);
}
