namespace Security.IntegrationTests.Contracts.Auth;

public sealed record MfaChallengeResponse(
    string ChallengeToken,
    DateTime ExpiresAtUtc);