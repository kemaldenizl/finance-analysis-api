namespace Security.API.Contracts.Auth;

public sealed record MfaChallengeResponse(string ChallengeToken, DateTime ExpiresAtUtc);