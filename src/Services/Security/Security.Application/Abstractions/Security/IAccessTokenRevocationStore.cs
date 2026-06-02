namespace Security.Application.Abstractions.Security;

public interface IAccessTokenRevocationStore
{
    Task RevokeAsync(
        string jti,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken = default
    );

    Task<bool> IsRevokedAsync(
        string jti,
        CancellationToken cancellationToken = default
    );

    Task RevokeUserAsync(
        Guid userId,
        DateTime invalidatedAtUtc,
        DateTime absoluteExpirationUtc,
        CancellationToken cancellationToken = default);

    Task<bool> IsUserTokenInvalidatedAsync(
        Guid userId,
        DateTime tokenIssuedAtUtc,
        CancellationToken cancellationToken = default);

    Task RevokeSessionAsync(
        Guid sessionId,
        DateTime invalidatedAtUtc,
        DateTime absoluteExpirationUtc,
        CancellationToken cancellationToken = default);

    Task<bool> IsSessionTokenInvalidatedAsync(
        Guid sessionId,
        DateTime tokenIssuedAtUtc,
        CancellationToken cancellationToken = default);
}