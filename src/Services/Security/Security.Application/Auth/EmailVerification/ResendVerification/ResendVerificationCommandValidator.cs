using FluentValidation;

namespace Security.Application.Auth.EmailVerification.ResendVerification;

public sealed class ResendVerificationCommandValidator : AbstractValidator<ResendVerificationCommand>
{
    public ResendVerificationCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .MaximumLength(320)
            .EmailAddress();
    }
}