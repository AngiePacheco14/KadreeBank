using FluentValidation;

namespace KadreeBank.Application.Accounts.Queries.GetRecentTransactions;

public sealed class GetRecentTransactionsQueryValidator : AbstractValidator<GetRecentTransactionsQuery>
{
    public GetRecentTransactionsQueryValidator()
    {
        RuleFor(x => x.AccountId).NotEmpty();
        RuleFor(x => x.Count).InclusiveBetween(1, 100);
    }
}
