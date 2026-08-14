using KadreeBank.Application.Accounts.Dtos;
using KadreeBank.Application.Services;
using MediatR;

namespace KadreeBank.Application.Accounts.Commands.CreateAccount;

public sealed class CreateAccountCommandHandler(IAccountService accountService)
    : IRequestHandler<CreateAccountCommand, AccountDto>
{
    public Task<AccountDto> Handle(CreateAccountCommand request, CancellationToken cancellationToken) =>
        accountService.CreateAccountAsync(request.CustomerId, request.Type, request.OriginCity, cancellationToken);
}
