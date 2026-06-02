using MediatR;
using Security.Application.Abstractions.Auditing;
using Security.Application.Abstractions.Persistence;
using Security.Application.Abstractions.Security;
using Security.Application.Abstractions.Time;
using Security.Application.Abstractions.UnitOfWork;
using Security.Application.Common.Auditing;
using Security.Application.Common.Errors;
using Security.Application.Common.Results;
using Security.Domain.Auditing;

namespace Security.Application.Auth.PasswordReset.ResetPassword;

public sealed class ResetPasswordCommandHandler(
    IUserRepository userRepository,
    IPasswordResetTokenRepository passwordResetTokenRepository,
    IAuditLogRepository auditLogRepository,
    IAuditLogFactory auditLogFactory,
    IPasswordResetTokenGenerator passwordResetTokenGenerator,
    IPasswordHasher passwordHasher,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ResetPasswordCommand, Result>
{
    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var utcNow = dateTimeProvider.UtcNow;
        var hashedToken = passwordResetTokenGenerator.Hash(request.Token);

        var resetToken = await passwordResetTokenRepository.GetByTokenHashAsync(hashedToken, cancellationToken);
            
        if (resetToken is null)
        {
            return Result.Failure(AuthErrors.InvalidPasswordResetToken);
        }

        if (resetToken.Used)
        {
            return Result.Failure(AuthErrors.UsedPasswordResetToken);
        }

        if (resetToken.IsExpired(utcNow))
        {
            return Result.Failure(AuthErrors.ExpiredPasswordResetToken);
        }

        var user = await userRepository.GetByIdAsync(resetToken.UserId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return Result.Failure(AuthErrors.InvalidPasswordResetToken);
        }

        user.ChangePasswordHash(passwordHasher.Hash(request.NewPassword));
        resetToken.MarkUsed(utcNow);

        var auditLog = auditLogFactory.Create(
            AuditActionType.PasswordResetCompleted,
            AuditPayloadBuilder.Build(new
            {
                @event = "password_reset_completed",
                userId = user.Id,
                email = user.Email
            }),
            user.Id);

        await auditLogRepository.AddAsync(auditLog, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}