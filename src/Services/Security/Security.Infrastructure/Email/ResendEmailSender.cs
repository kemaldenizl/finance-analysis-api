using System.Net;
using Microsoft.Extensions.Options;
using Resend;
using Security.Application.Abstractions.Email;

namespace Security.Infrastructure.Email;

public sealed class ResendEmailSender(IOptions<ResendEmailOptions> options) : IEmailSender
{
    private readonly ResendEmailOptions _options = options.Value;

    public async Task SendEmailVerificationAsync(
        string to,
        string verificationToken,
        CancellationToken cancellationToken = default)
    {
        ValidateOptions();

        var verificationUrl = BuildUrl(_options.VerifyEmailPath, verificationToken);

        IResend resend = ResendClient.Create(_options.ApiKey);

        await resend.EmailSendAsync(new EmailMessage
        {
            From = BuildFrom(),
            To = to,
            Subject = "Verify your email address",
            HtmlBody = BuildEmailVerificationHtml(verificationUrl)
        });
    }

    public async Task SendPasswordResetAsync(
        string to,
        string resetToken,
        CancellationToken cancellationToken = default)
    {
        ValidateOptions();

        var resetUrl = BuildUrl(_options.ResetPasswordPath, resetToken);

        IResend resend = ResendClient.Create(_options.ApiKey);

        await resend.EmailSendAsync(new EmailMessage
        {
            From = BuildFrom(),
            To = to,
            Subject = "Reset your password",
            HtmlBody = BuildPasswordResetHtml(resetUrl)
        });
    }

    private string BuildFrom()
    {
        return string.IsNullOrWhiteSpace(_options.FromName)
            ? _options.FromEmail
            : $"{_options.FromName} <{_options.FromEmail}>";
    }

    private string BuildUrl(string path, string token)
    {
        var baseUrl = _options.FrontendBaseUrl.TrimEnd('/');
        var safePath = path.StartsWith('/') ? path : "/" + path;
        var encodedToken = WebUtility.UrlEncode(token);

        return $"{baseUrl}{safePath}?token={encodedToken}";
    }

    private static string BuildEmailVerificationHtml(string verificationUrl)
    {
        var encodedUrl = WebUtility.HtmlEncode(verificationUrl);

        return $$"""
        <div style="font-family:Arial,sans-serif;line-height:1.6">
            <h2>Verify your email address</h2>
            <p>Click the button below to verify your email address.</p>
            <p>
                <a href="{{encodedUrl}}" style="display:inline-block;padding:10px 16px;background:#111;color:#fff;text-decoration:none;border-radius:6px">
                    Verify Email
                </a>
            </p>
            <p>If the button does not work, copy and paste this link into your browser:</p>
            <p><a href="{{encodedUrl}}">{{encodedUrl}}</a></p>
            <p>This link will expire soon.</p>
        </div>
        """;
    }

    private static string BuildPasswordResetHtml(string resetUrl)
    {
        var encodedUrl = WebUtility.HtmlEncode(resetUrl);

        return $$"""
        <div style="font-family:Arial,sans-serif;line-height:1.6">
            <h2>Reset your password</h2>
            <p>Click the button below to reset your password.</p>
            <p>
                <a href="{{encodedUrl}}" style="display:inline-block;padding:10px 16px;background:#111;color:#fff;text-decoration:none;border-radius:6px">
                    Reset Password
                </a>
            </p>
            <p>If the button does not work, copy and paste this link into your browser:</p>
            <p><a href="{{encodedUrl}}">{{encodedUrl}}</a></p>
            <p>If you did not request this, you can ignore this email.</p>
        </div>
        """;
    }

    private void ValidateOptions()
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException("Resend:ApiKey is missing.");

        if (string.IsNullOrWhiteSpace(_options.FromEmail))
            throw new InvalidOperationException("Resend:FromEmail is missing.");

        if (string.IsNullOrWhiteSpace(_options.FrontendBaseUrl))
            throw new InvalidOperationException("Resend:FrontendBaseUrl is missing.");
    }
}