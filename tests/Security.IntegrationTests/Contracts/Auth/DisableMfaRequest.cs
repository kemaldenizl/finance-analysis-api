namespace Security.IntegrationTests.Contracts.Auth;

public sealed record DisableMfaRequest(
    string? TotpCode,
    string? RecoveryCode);