using FluentValidation;

namespace KadreeBank.Application.Reports.Queries.GetCustomerTransactionCounts;

public sealed class GetCustomerTransactionCountsQueryValidator : AbstractValidator<GetCustomerTransactionCountsQuery>
{
    public GetCustomerTransactionCountsQueryValidator()
    {
        RuleFor(x => x.Year).InclusiveBetween(2000, 2100);
        RuleFor(x => x.Month).InclusiveBetween(1, 12);
    }
}
