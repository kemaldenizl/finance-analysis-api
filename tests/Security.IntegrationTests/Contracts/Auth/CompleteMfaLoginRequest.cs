namespace Security.IntegrationTests.Contracts.Auth;

public sealed record CompleteMfaLoginRequest(
    string ChallengeToken,
    string? TotpCode,
    string? RecoveryCode);