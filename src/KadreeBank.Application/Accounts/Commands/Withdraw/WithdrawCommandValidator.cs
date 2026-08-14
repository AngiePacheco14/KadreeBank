using FluentValidation;

namespace KadreeBank.Application.Accounts.Commands.Withdraw;

public sealed class WithdrawCommandValidator : AbstractValidator<WithdrawCommand>
{
    public WithdrawCommandValidator()
    {
        RuleFor(x => x.AccountId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
    }
}
