namespace Security.Application.Abstractions.Security;

public interface IRedisRevocationStore
{
    Task RevokeAccessTokenAsync(string jti, DateTime expiresAtUtc, CancellationToken cancellationToken);
    Task<bool> IsRevokedAsync(string jti, CancellationToken cancellationToken);
}

//Kullanılmıyor iptal edilecek...
