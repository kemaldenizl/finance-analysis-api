namespace Security.Infrastructure.RateLimiting;

public static class RateLimitPolicyNames
{
    public const string Register = "rate-limit:register";
    public const string Login = "rate-limit:login";
    public const string Refresh = "rate-limit:refresh";
    public const string Logout = "rate-limit:logout";
    public const string Sessions = "rate-limit:sessions";
    public const string ForgotPassword = "rate-limit:forgot-password";
    public const string ResetPassword = "rate-limit:reset-password";
    public const string VerifyEmail = "rate-limit:verify-email";
    public const string ResendVerification = "rate-limit:resend-verification";
}