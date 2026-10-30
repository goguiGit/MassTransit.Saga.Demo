using FluentValidation;

namespace Registration.Application.Features.ConfirmEmail;

public sealed class ConfirmEmailCommandValidator : AbstractValidator<ConfirmEmailCommand>
{
    public ConfirmEmailCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Code).Matches($"^[0-9]{{{VerificationCode.Length}}}$").WithMessage($"Code must be {VerificationCode.Length} digits.");
    }
}
