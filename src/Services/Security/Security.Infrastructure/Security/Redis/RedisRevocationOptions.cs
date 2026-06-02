namespace Security.Infrastructure.Security.Redis;

public sealed class RedisRevocationOptions
{
    public const string SectionName = "RedisRevocation";
    public string InstanceName { get; init; } = "security";
    public string AccessTokenRevocationPrefix { get; init; } = "revoked:access:jti:";
    public string UserInvalidationPrefix { get; init; } = "revoked:access:user:";
    public string SessionInvalidationPrefix { get; init; } = "revoked:access:session:";

    public int UserInvalidationRetentionHours { get; init; } = 24;
    public int SessionInvalidationRetentionHours { get; init; } = 24;
}