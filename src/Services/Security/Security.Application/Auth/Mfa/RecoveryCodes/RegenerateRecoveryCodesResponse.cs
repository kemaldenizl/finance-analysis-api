namespace Security.Application.Auth.Mfa.RecoveryCodes;

public sealed record RegenerateRecoveryCodesResponse(
    IReadOnlyCollection<string> RecoveryCodes);