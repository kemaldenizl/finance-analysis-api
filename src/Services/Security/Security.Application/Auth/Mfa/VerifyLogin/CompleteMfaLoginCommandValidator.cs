using FluentValidation;

namespace Security.Application.Auth.Mfa.VerifyLogin;

public sealed class CompleteMfaLoginCommandValidator : AbstractValidator<CompleteMfaLoginCommand>
{
    public CompleteMfaLoginCommandValidator()
    {
        RuleFor(x => x.ChallengeToken)
            .NotEmpty();

        RuleFor(x => x)
            .Must(x =>
                !string.IsNullOrWhiteSpace(x.TotpCode) ||
                !string.IsNullOrWhiteSpace(x.RecoveryCode))
            .WithMessage("Either TOTP code or recovery code must be provided.");
    }
}