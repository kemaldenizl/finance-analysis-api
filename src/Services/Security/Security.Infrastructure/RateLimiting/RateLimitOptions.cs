namespace Security.Infrastructure.RateLimiting;

public sealed class RateLimitOptions
{
    public const string SectionName = "RateLimiting";

    public FixedWindowPolicyOptions Register { get; init; } = new();
    public FixedWindowPolicyOptions Login { get; init; } = new();
    public FixedWindowPolicyOptions Refresh { get; init; } = new();
    public FixedWindowPolicyOptions Logout { get; init; } = new();
    public FixedWindowPolicyOptions Sessions { get; init; } = new();
    public FixedWindowPolicyOptions ForgotPassword { get; init; } = new();
    public FixedWindowPolicyOptions ResetPassword { get; init; } = new();
}

public sealed class FixedWindowPolicyOptions
{
    public int PermitLimit { get; init; } = 5;
    public int WindowSeconds { get; init; } = 60;
    public int QueueLimit { get; init; } = 0;
    public bool AutoReplenishment { get; init; } = true;
}