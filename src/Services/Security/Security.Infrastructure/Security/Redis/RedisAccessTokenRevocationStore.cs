using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Security.Application.Abstractions.Security;

namespace Security.Infrastructure.Security.Redis;

public sealed class RedisAccessTokenRevocationStore(
    IDistributedCache distributedCache,
    IOptions<RedisRevocationOptions> options
)   : IAccessTokenRevocationStore
{
    private readonly IDistributedCache _distributedCache = distributedCache;
    private readonly RedisRevocationOptions _options = options.Value;

    public async Task RevokeAsync(
        string jti,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jti);

        var ttl = CalculateTtl(expiresAtUtc);
        var key = BuildJtiKey(jti);

        await _distributedCache.SetStringAsync(
            key,
            "1",
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl
            },
            cancellationToken);
    }

    public async Task<bool> IsRevokedAsync(
        string jti,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jti);

        var value = await _distributedCache.GetStringAsync(
            BuildJtiKey(jti),
            cancellationToken);

        return !string.IsNullOrWhiteSpace(value);
    }

    public async Task RevokeUserAsync(
        Guid userId,
        DateTime invalidatedAtUtc,
        DateTime absoluteExpirationUtc,
        CancellationToken cancellationToken = default)
    {
        var ttl = CalculateTtl(absoluteExpirationUtc);
        var key = BuildUserInvalidationKey(userId);

        await _distributedCache.SetStringAsync(
            key,
            invalidatedAtUtc.Ticks.ToString(),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl
            },
            cancellationToken);
    }

    public async Task<bool> IsUserTokenInvalidatedAsync(
        Guid userId,
        DateTime tokenIssuedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var value = await _distributedCache.GetStringAsync(
            BuildUserInvalidationKey(userId),
            cancellationToken);

        if (string.IsNullOrWhiteSpace(value))
            return false;

        if (!long.TryParse(value, out var ticks))
            return false;

        var invalidatedAtUtc = new DateTime(ticks, DateTimeKind.Utc);

        return tokenIssuedAtUtc <= invalidatedAtUtc;
    }

    public async Task RevokeSessionAsync(
        Guid sessionId,
        DateTime invalidatedAtUtc,
        DateTime absoluteExpirationUtc,
        CancellationToken cancellationToken = default)
    {
        var ttl = CalculateTtl(absoluteExpirationUtc);
        var key = BuildSessionInvalidationKey(sessionId);

        await _distributedCache.SetStringAsync(
            key,
            invalidatedAtUtc.Ticks.ToString(),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl
            },
            cancellationToken);
    }

    public async Task<bool> IsSessionTokenInvalidatedAsync(
        Guid sessionId,
        DateTime tokenIssuedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var value = await _distributedCache.GetStringAsync(
            BuildSessionInvalidationKey(sessionId),
            cancellationToken);

        if (string.IsNullOrWhiteSpace(value))
            return false;

        if (!long.TryParse(value, out var ticks))
            return false;

        var invalidatedAtUtc = new DateTime(ticks, DateTimeKind.Utc);

        return tokenIssuedAtUtc <= invalidatedAtUtc;
    }

    private static TimeSpan CalculateTtl(DateTime absoluteExpirationUtc)
    {
        var ttl = absoluteExpirationUtc - DateTime.UtcNow;

        return ttl <= TimeSpan.Zero
            ? TimeSpan.FromMinutes(1)
            : ttl;
    }

    private string BuildJtiKey(string jti)
    {
        return $"{_options.InstanceName}:{_options.AccessTokenRevocationPrefix}{jti}";
    }

    private string BuildUserInvalidationKey(Guid userId)
    {
        return $"{_options.InstanceName}:{_options.UserInvalidationPrefix}{userId:N}";
    }

    private string BuildSessionInvalidationKey(Guid sessionId)
    {
        return $"{_options.InstanceName}:{_options.SessionInvalidationPrefix}{sessionId:N}";
    }
}