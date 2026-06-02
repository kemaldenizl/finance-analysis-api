using FluentValidation;

namespace Security.Application.Auth.Mfa.Disable;

public sealed class DisableMfaCommandValidator : AbstractValidator<DisableMfaCommand>
{
    public DisableMfaCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();

        RuleFor(x => x)
            .Must(x =>
                !string.IsNullOrWhiteSpace(x.TotpCode) ||
                !string.IsNullOrWhiteSpace(x.RecoveryCode))
            .WithMessage("Either TOTP code or recovery code must be provided.");

        When(x => !string.IsNullOrWhiteSpace(x.TotpCode), () =>
        {
            RuleFor(x => x.TotpCode!)
                .Length(6, 8)
                .Matches("^[0-9]+$");
        });

        When(x => !string.IsNullOrWhiteSpace(x.RecoveryCode), () =>
        {
            RuleFor(x => x.RecoveryCode!)
                .MinimumLength(8)
                .MaximumLength(64);
        });
    }
}