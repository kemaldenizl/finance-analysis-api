using MediatR;
using Security.Application.Abstractions.Auditing;
using Security.Application.Abstractions.Persistence;
using Security.Application.Abstractions.Security;
using Security.Application.Abstractions.Time;
using Security.Application.Abstractions.UnitOfWork;
using Security.Application.Auth.Dtos;
using Security.Application.Common.Auditing;
using Security.Application.Common.Errors;
using Security.Application.Common.Results;
using Security.Domain.Auditing;
using Security.Domain.Tokens;
using Security.Domain.Users;

namespace Security.Application.Auth.Register;

public sealed class RegisterCommandHandler(
    IUserRepository userRepository,
    IEmailVerificationTokenRepository emailVerificationTokenRepository,
    IAuditLogRepository auditLogRepository,
    IAuditLogFactory auditLogFactory,
    IPasswordHasher passwordHasher,
    IEmailVerificationTokenGenerator emailVerificationTokenGenerator,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RegisterCommand, Result<RegisterResponse>>
{
    private static readonly TimeSpan VerificationTokenLifetime = TimeSpan.FromHours(24);

    public async Task<Result<RegisterResponse>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();

        var alreadyExists = await userRepository.ExistsByNormalizedEmailAsync(normalizedEmail, cancellationToken);
        if (alreadyExists)
        {
            return Result<RegisterResponse>.Failure(AuthErrors.UserAlreadyExists);
        }

        var utcNow = dateTimeProvider.UtcNow;

        var user = new User(
            Guid.NewGuid(),
            request.Email.Trim(),
            normalizedEmail,
            passwordHasher.Hash(request.Password),
            utcNow);

        await userRepository.AddAsync(user, cancellationToken);

        var tokenPair = emailVerificationTokenGenerator.Generate();

        var verificationToken = new EmailVerificationToken(
            Guid.NewGuid(),
            user.Id,
            tokenPair.HashedToken,
            utcNow.Add(VerificationTokenLifetime),
            utcNow);

        await emailVerificationTokenRepository.AddAsync(verificationToken, cancellationToken);

        var auditLog = auditLogFactory.Create(
            AuditActionType.UserRegistered,
            AuditPayloadBuilder.Build(new
            {
                @event = "user_registered",
                userId = user.Id,
                email = user.Email,
                emailVerificationStarted = true
            }),
            user.Id);

        await auditLogRepository.AddAsync(auditLog, cancellationToken);

        var verificationAuditLog = auditLogFactory.Create(
            AuditActionType.EmailVerificationRequested,
            AuditPayloadBuilder.Build(new
            {
                @event = "email_verification_requested",
                userId = user.Id,
                email = user.Email,
                reason = "register"
            }),
            user.Id);

        await auditLogRepository.AddAsync(verificationAuditLog, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var response = new RegisterResponse(new UserDto(user.Id, user.Email, user.EmailVerified, user.IsActive));
        return Result<RegisterResponse>.Success(response);
    }
}