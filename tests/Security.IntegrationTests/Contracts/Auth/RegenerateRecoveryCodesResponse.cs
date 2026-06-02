namespace Security.IntegrationTests.Contracts.Auth;

public sealed record RegenerateRecoveryCodesResponse(
    IReadOnlyCollection<string> RecoveryCodes);