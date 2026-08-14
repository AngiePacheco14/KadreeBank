using FluentValidation;

namespace KadreeBank.Application.Accounts.Commands.CreateAccount;

public sealed class CreateAccountCommandValidator : AbstractValidator<CreateAccountCommand>
{
    public CreateAccountCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Type).IsInEnum();

        RuleFor(x => x.OriginCity)
            .NotEmpty()
            .MaximumLength(100);
    }
}
