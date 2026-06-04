using MediatR;
using Microsoft.Extensions.Logging;
using Security.Application.Abstractions.Auditing;
using Security.Application.Abstractions.Email;
using Security.Application.Abstractions.Persistence;
using Security.Application.Abstractions.Security;
using Security.Application.Abstractions.Time;
using Security.Application.Abstractions.UnitOfWork;
using Security.Application.Auth.EmailVerification.Dtos;
using Security.Application.Common.Auditing;
using Security.Application.Common.Results;
using Security.Domain.Auditing;
using Security.Domain.Tokens;

namespace Security.Application.Auth.EmailVerification.ResendVerification;

public sealed class ResendVerificationCommandHandler(
    IUserRepository userRepository,
    IEmailVerificationTokenRepository emailVerificationTokenRepository,
    IAuditLogRepository auditLogRepository,
    IAuditLogFactory auditLogFactory,
    IEmailVerificationTokenGenerator emailVerificationTokenGenerator,
    IEmailSender emailSender,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork,
    ILogger<ResendVerificationCommandHandler> logger)
    : IRequestHandler<ResendVerificationCommand, Result<ResendVerificationResponse>>
{
    private static readonly TimeSpan VerificationTokenLifetime = TimeSpan.FromHours(24);

    public async Task<Result<ResendVerificationResponse>> Handle(
        ResendVerificationCommand request,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        var user = await userRepository.GetByNormalizedEmailAsync(normalizedEmail, cancellationToken);

        if (user is not null && user.IsActive && !user.EmailVerified)
        {
            var utcNow = dateTimeProvider.UtcNow;
            var tokenPair = emailVerificationTokenGenerator.Generate();

            var verificationToken = new EmailVerificationToken(
                Guid.NewGuid(),
                user.Id,
                tokenPair.HashedToken,
                utcNow.Add(VerificationTokenLifetime),
                utcNow);

            await emailVerificationTokenRepository.AddAsync(verificationToken, cancellationToken);

            var auditLog = auditLogFactory.Create(
                AuditActionType.EmailVerificationRequested,
                AuditPayloadBuilder.Build(new
                {
                    @event = "email_verification_requested",
                    userId = user.Id,
                    email = user.Email,
                    reason = "resend"
                }),
                user.Id);

            await auditLogRepository.AddAsync(auditLog, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            try
            {
                await emailSender.SendEmailVerificationAsync(
                    user.Email,
                    tokenPair.PlainTextToken,
                    cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Email verification resend mail could not be sent. UserId: {UserId}",
                    user.Id);
            }
        }

        return Result<ResendVerificationResponse>.Success(
            new ResendVerificationResponse(
                "If the account exists and is not verified, a new verification link has been generated."));
    }
}