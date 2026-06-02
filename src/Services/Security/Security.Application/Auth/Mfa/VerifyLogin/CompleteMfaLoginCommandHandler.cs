using MediatR;
using Security.Application.Abstractions.Authentication;
using Security.Application.Abstractions.Auditing;
using Security.Application.Abstractions.Persistence;
using Security.Application.Abstractions.Security;
using Security.Application.Abstractions.Time;
using Security.Application.Abstractions.UnitOfWork;
using Security.Application.Auth.Dtos;
using Security.Application.Auth.Login;
using Security.Application.Common.Auditing;
using Security.Application.Common.Results;
using Security.Domain.Auditing;

namespace Security.Application.Auth.Mfa.VerifyLogin;

public sealed class CompleteMfaLoginCommandHandler(
    IUserRepository userRepository,
    IMfaMethodRepository mfaMethodRepository,
    IRefreshSessionRepository refreshSessionRepository,
    IAuditLogRepository auditLogRepository,
    IAuditLogFactory auditLogFactory,
    ITokenGenerator tokenGenerator,
    ITotpSecretProtector totpSecretProtector,
    ITotpService totpService,
    IRecoveryCodeService recoveryCodeService,
    IMfaChallengeTokenService mfaChallengeTokenService,
    IRoleRepository roleRepository,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CompleteMfaLoginCommand, Result<LoginResponse>>
{
    public async Task<Result<LoginResponse>> Handle(
        CompleteMfaLoginCommand request,
        CancellationToken cancellationToken)
    {
        var payload = mfaChallengeTokenService.Validate(request.ChallengeToken);
        if (payload is null || payload.ExpiresAtUtc <= dateTimeProvider.UtcNow)
        {
            return Result<LoginResponse>.Failure(
                new("auth.invalid_mfa_challenge", "The MFA challenge token is invalid or expired."));
        }

        var user = await userRepository.GetByIdAsync(payload.UserId, cancellationToken);
        var mfaMethod = await mfaMethodRepository.GetByUserIdAsync(payload.UserId, cancellationToken);
        var session = await refreshSessionRepository.GetByIdAsync(payload.SessionId, cancellationToken);

        if (user is null || mfaMethod is null || session is null || !mfaMethod.IsEnabled || !mfaMethod.IsVerified)
        {
            return Result<LoginResponse>.Failure(
                new("auth.invalid_mfa_challenge", "The MFA challenge token is invalid or expired."));
        }

        var utcNow = dateTimeProvider.UtcNow;
        var verified = false;
        var usedRecoveryCode = false;

        if (!string.IsNullOrWhiteSpace(request.TotpCode))
        {
            var secret = totpSecretProtector.Unprotect(mfaMethod.SecretEncrypted);
            verified = totpService.VerifyCode(secret, request.TotpCode);
        }
        else if (!string.IsNullOrWhiteSpace(request.RecoveryCode))
        {
            var recoveryCode = mfaMethod.GetUsableRecoveryCodeByHash(
                recoveryCodeService.Hash(request.RecoveryCode));

            if (recoveryCode is not null)
            {
                recoveryCode.MarkUsed(utcNow);
                verified = true;
                usedRecoveryCode = true;
            }
        }

        if (!verified)
        {
            return Result<LoginResponse>.Failure(
                new("auth.invalid_mfa_code", "The MFA code is invalid."));
        }

        var permissions = await roleRepository.GetPermissionCodesByUserIdAsync(user.Id, cancellationToken);

        var accessToken = await tokenGenerator.GenerateAccessTokenAsync(
            user.Id,
            user.Email,
            permissions,
            session.Id,
            cancellationToken);

        var latestRefreshToken = session.GetLatestActiveToken(utcNow);
        if (latestRefreshToken is null)
        {
            return Result<LoginResponse>.Failure(
                new("auth.invalid_session", "The login session is invalid."));
        }

        var auditType = usedRecoveryCode
            ? AuditActionType.MfaRecoveryCodeUsed
            : AuditActionType.MfaLoginCompleted;

        var auditLog = auditLogFactory.Create(
            auditType,
            AuditPayloadBuilder.Build(new
            {
                @event = usedRecoveryCode ? "mfa_recovery_code_used" : "mfa_login_completed",
                userId = user.Id,
                sessionId = session.Id
            }),
            user.Id);

        await auditLogRepository.AddAsync(auditLog, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<LoginResponse>.Success(
            new LoginResponse(
                new UserDto(user.Id, user.Email, user.EmailVerified, user.IsActive),
                new AuthTokensDto(
                    accessToken.AccessToken,
                    accessToken.AccessTokenExpiresAtUtc,
                    string.Empty, // burada plaintext refresh token elimizde yok
                    latestRefreshToken.ExpiresAtUtc),
                null,
                false));
    }
}