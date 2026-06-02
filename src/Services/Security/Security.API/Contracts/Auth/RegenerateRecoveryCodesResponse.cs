namespace Security.API.Contracts.Auth;

public sealed record RegenerateRecoveryCodesResponse(
    IReadOnlyCollection<string> RecoveryCodes);