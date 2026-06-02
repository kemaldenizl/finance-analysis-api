namespace Security.IntegrationTests.Contracts.Auth;

public sealed record RegenerateRecoveryCodesRequest(
    string TotpCode);