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

namespace Security.Application.Auth.EmailVerification.VerifyEmail;

public sealed class VerifyEmailCommandHandler(
    IUserRepository userRepository,
    IEmailVerificationTokenRepository emailVerificationTokenRepository,
    IAuditLogRepository auditLogRepository,
    IAuditLogFactory auditLogFactory,
    IEmailVerificationTokenGenerator emailVerificationTokenGenerator,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
    : IRequestHandler<VerifyEmailCommand, Result>
{
    public async Task<Result> Handle(
        VerifyEmailCommand request,
        CancellationToken cancellationToken)
    {
        var utcNow = dateTimeProvider.UtcNow;
        var hashedToken = emailVerificationTokenGenerator.Hash(request.Token);

        var verificationToken = await emailVerificationTokenRepository.GetByTokenHashAsync(
            hashedToken,
            cancellationToken);

        if (verificationToken is null)
        {
            return Result.Failure(AuthErrors.InvalidEmailVerificationToken);
        }

        if (verificationToken.Used)
        {
            return Result.Failure(AuthErrors.UsedEmailVerificationToken);
        }

        if (verificationToken.IsExpired(utcNow))
        {
            return Result.Failure(AuthErrors.ExpiredEmailVerificationToken);
        }

        var user = await userRepository.GetByIdAsync(verificationToken.UserId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return Result.Failure(AuthErrors.InvalidEmailVerificationToken);
        }

        if (user.EmailVerified)
        {
            return Result.Failure(AuthErrors.EmailAlreadyVerified);
        }

        user.MarkEmailVerified();
        verificationToken.MarkUsed(utcNow);

        var auditLog = auditLogFactory.Create(
            AuditActionType.EmailVerified,
            AuditPayloadBuilder.Build(new
            {
                @event = "email_verified",
                userId = user.Id,
                email = user.Email
            }),
            user.Id);

        await auditLogRepository.AddAsync(auditLog, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}