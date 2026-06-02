namespace Security.Application.Abstractions.Security;

public sealed class SecurityTokenInvalidationOptions
{
    public const string SectionName = "SecurityTokenInvalidation";

    public int UserInvalidationRetentionHours { get; init; } = 24;
    public int SessionInvalidationRetentionHours { get; init; } = 24;
}