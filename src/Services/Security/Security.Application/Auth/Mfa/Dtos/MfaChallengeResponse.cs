namespace Security.Application.Auth.Mfa.Dtos;

public sealed record MfaChallengeResponse(string ChallengeToken, DateTime ExpiresAtUtc);