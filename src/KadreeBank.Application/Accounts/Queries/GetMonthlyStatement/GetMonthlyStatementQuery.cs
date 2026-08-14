using KadreeBank.Application.Accounts.Dtos;
using MediatR;

namespace KadreeBank.Application.Accounts.Queries.GetMonthlyStatement;

public sealed record GetMonthlyStatementQuery(Guid AccountId, int Year, int Month) : IRequest<MonthlyStatementDto>;
