using FluentValidation;

namespace KadreeBank.Application.Accounts.Commands.Deposit;

public sealed class DepositCommandValidator : AbstractValidator<DepositCommand>
{
    public DepositCommandValidator()
    {
        RuleFor(x => x.AccountId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
    }
}
