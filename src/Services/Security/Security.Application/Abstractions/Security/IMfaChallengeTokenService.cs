namespace Security.Application.Abstractions.Security;

public interface IMfaChallengeTokenService
{
    string Create(Guid userId, Guid sessionId, string refreshToken, DateTime expiresAtUtc);

    MfaChallengeTokenPayload? Validate(string token);
}

public sealed record MfaChallengeTokenPayload(
    Guid UserId,
    Guid SessionId,
    string RefreshToken,
    DateTime ExpiresAtUtc);