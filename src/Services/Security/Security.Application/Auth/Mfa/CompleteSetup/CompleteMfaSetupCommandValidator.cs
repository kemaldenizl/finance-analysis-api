using FluentValidation;

namespace Security.Application.Auth.Mfa.CompleteSetup;

public sealed class CompleteMfaSetupCommandValidator : AbstractValidator<CompleteMfaSetupCommand>
{
    public CompleteMfaSetupCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();

        RuleFor(x => x.Code)
            .NotEmpty()
            .Length(6, 8)
            .Matches("^[0-9]+$");
    }
}