using KadreeBank.Application.Accounts.Dtos;
using KadreeBank.Application.Services;
using MediatR;

namespace KadreeBank.Application.Accounts.Queries.GetMonthlyStatement;

public sealed class GetMonthlyStatementQueryHandler(IAccountService accountService)
    : IRequestHandler<GetMonthlyStatementQuery, MonthlyStatementDto>
{
    public Task<MonthlyStatementDto> Handle(GetMonthlyStatementQuery request, CancellationToken cancellationToken) =>
        accountService.GetMonthlyStatementAsync(request.AccountId, request.Year, request.Month, cancellationToken);
}
