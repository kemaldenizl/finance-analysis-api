using FluentValidation;

namespace Security.Application.Auth.Mfa.RecoveryCodes;

public sealed class RegenerateRecoveryCodesCommandValidator : AbstractValidator<RegenerateRecoveryCodesCommand>
{
    public RegenerateRecoveryCodesCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();

        RuleFor(x => x.TotpCode)
            .NotEmpty()
            .Length(6, 8)
            .Matches("^[0-9]+$");
    }
}