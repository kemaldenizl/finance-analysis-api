namespace Security.Infrastructure.Email;

public sealed class ResendEmailOptions
{
    public const string SectionName = "Resend";

    public string ApiKey { get; init; } = string.Empty;

    public string FromEmail { get; init; } = "onboarding@resend.dev";

    public string FromName { get; init; } = "Security Service";

    public string FrontendBaseUrl { get; init; } = "http://localhost:3000";

    public string VerifyEmailPath { get; init; } = "/verify-email";

    public string ResetPasswordPath { get; init; } = "/reset-password";
}