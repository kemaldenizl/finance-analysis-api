using MediatR;
using Security.Application.Abstractions.Auditing;
using Security.Application.Abstractions.Persistence;
using Security.Application.Abstractions.Security;
using Security.Application.Abstractions.Time;
using Security.Application.Abstractions.UnitOfWork;
using Security.Application.Auth.PasswordReset.Dtos;
using Security.Application.Common.Auditing;
using Security.Application.Common.Results;
using Security.Domain.Auditing;
using Security.Domain.Tokens;

namespace Security.Application.Auth.PasswordReset.ForgotPassword;

public sealed class ForgotPasswordCommandHandler(
    IUserRepository userRepository,
    IPasswordResetTokenRepository passwordResetTokenRepository,
    IAuditLogRepository auditLogRepository,
    IAuditLogFactory auditLogFactory,
    IPasswordResetTokenGenerator passwordResetTokenGenerator,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ForgotPasswordCommand, Result<ForgotPasswordResponse>>
{
    private static readonly TimeSpan PasswordResetTokenLifetime = TimeSpan.FromMinutes(30);

    public async Task<Result<ForgotPasswordResponse>> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        var user = await userRepository.GetByNormalizedEmailAsync(normalizedEmail, cancellationToken);

        if (user is not null && user.IsActive)
        {
            var utcNow = dateTimeProvider.UtcNow;
            var tokenPair = passwordResetTokenGenerator.Generate();

            var resetToken = new PasswordResetToken(
                Guid.NewGuid(),
                user.Id,
                tokenPair.HashedToken,
                utcNow.Add(PasswordResetTokenLifetime),
                utcNow);

            await passwordResetTokenRepository.AddAsync(resetToken, cancellationToken);

            var auditLog = auditLogFactory.Create(
                AuditActionType.PasswordResetRequested,
                AuditPayloadBuilder.Build(new
                {
                    @event = "password_reset_requested",
                    userId = user.Id,
                    email = user.Email
                }),
                user.Id);

            await auditLogRepository.AddAsync(auditLog, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            // Burada gerçek sistemde email dispatch/outbox olacak.
            // Şimdilik token taşıması API response'a konmuyor.
        }

        return Result<ForgotPasswordResponse>.Success(new ForgotPasswordResponse("If the account exists, a password reset link has been generated."));
    }
}