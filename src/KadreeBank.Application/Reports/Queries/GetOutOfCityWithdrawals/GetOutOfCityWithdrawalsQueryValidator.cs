using FluentValidation;

namespace KadreeBank.Application.Reports.Queries.GetOutOfCityWithdrawals;

public sealed class GetOutOfCityWithdrawalsQueryValidator : AbstractValidator<GetOutOfCityWithdrawalsQuery>
{
    public GetOutOfCityWithdrawalsQueryValidator()
    {
        RuleFor(x => x.MinAmount).GreaterThanOrEqualTo(0);
    }
}
