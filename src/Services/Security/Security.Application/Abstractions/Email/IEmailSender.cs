namespace Security.Application.Abstractions.Email;

public interface IEmailSender
{
    Task SendEmailVerificationAsync(
        string to,
        string verificationToken,
        CancellationToken cancellationToken = default);

    Task SendPasswordResetAsync(
        string to,
        string resetToken,
        CancellationToken cancellationToken = default);
}