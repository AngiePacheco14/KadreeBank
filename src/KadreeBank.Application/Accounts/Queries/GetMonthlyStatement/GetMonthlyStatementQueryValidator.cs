using FluentValidation;

namespace KadreeBank.Application.Accounts.Queries.GetMonthlyStatement;

public sealed class GetMonthlyStatementQueryValidator : AbstractValidator<GetMonthlyStatementQuery>
{
    public GetMonthlyStatementQueryValidator()
    {
        RuleFor(x => x.AccountId).NotEmpty();
        RuleFor(x => x.Year).InclusiveBetween(2000, 2100);
        RuleFor(x => x.Month).InclusiveBetween(1, 12);
    }
}
