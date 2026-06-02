using MediatR;
using Security.Application.Abstractions.Authentication;
using Security.Application.Abstractions.Auditing;
using Security.Application.Abstractions.Persistence;
using Security.Application.Abstractions.RequestContext;
using Security.Application.Abstractions.Security;
using Security.Application.Abstractions.Time;
using Security.Application.Abstractions.UnitOfWork;
using Security.Application.Auth.Dtos;
using Security.Application.Auth.Mfa.Dtos;
using Security.Application.Common.Auditing;
using Security.Application.Common.Errors;
using Security.Application.Common.Results;
using Security.Domain.Auditing;
using Security.Domain.Sessions;

namespace Security.Application.Auth.Login;

public sealed class LoginCommandHandler(
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    IRefreshSessionRepository refreshSessionRepository,
    IMfaMethodRepository mfaMethodRepository,
    IAuditLogRepository auditLogRepository,
    IAuditLogFactory auditLogFactory,
    IRequestContext requestContext,
    IPasswordHasher passwordHasher,
    IRefreshTokenGenerator refreshTokenGenerator,
    ITokenGenerator tokenGenerator,
    IMfaChallengeTokenService mfaChallengeTokenService,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
    : IRequestHandler<LoginCommand, Result<LoginResponse>>
{
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(30);
    private static readonly TimeSpan MfaChallengeLifetime = TimeSpan.FromMinutes(5);
    public async Task<Result<LoginResponse>> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();

        var user = await userRepository.GetByNormalizedEmailAsync(
            normalizedEmail,
            cancellationToken);

        if (user is null)
        {
            await WriteFailedLoginAuditAsync(normalizedEmail, cancellationToken);
            return Result<LoginResponse>.Failure(AuthErrors.InvalidCredentials);
        }

        if (!user.IsActive)
        {
            await WriteFailedLoginAuditAsync(normalizedEmail, cancellationToken);
            return Result<LoginResponse>.Failure(AuthErrors.UserInactive);
        }

        var passwordValid = passwordHasher.Verify(user.PasswordHash, request.Password);
        if (!passwordValid)
        {
            await WriteFailedLoginAuditAsync(normalizedEmail, cancellationToken);
            return Result<LoginResponse>.Failure(AuthErrors.InvalidCredentials);
        }

        var utcNow = dateTimeProvider.UtcNow;
        user.MarkLogin(utcNow);

        var refreshTokenPair = refreshTokenGenerator.Generate();
        var refreshTokenExpiresAtUtc = utcNow.Add(RefreshTokenLifetime);

        var session = new RefreshSession(
            Guid.NewGuid(),
            user.Id,
            requestContext.UserAgent,
            requestContext.IpAddress,
            utcNow);

        var refreshToken = new RefreshToken(
            Guid.NewGuid(),
            session.Id,
            refreshTokenPair.HashedToken,
            refreshTokenExpiresAtUtc,
            utcNow);

        session.AddToken(refreshToken);

        var existingMfa = await mfaMethodRepository.GetByUserIdAsync(user.Id, cancellationToken);

        if (existingMfa is { IsEnabled: true, IsVerified: true })
        {
            var challengeExpiresAtUtc = utcNow.Add(MfaChallengeLifetime);

            var challengeToken = mfaChallengeTokenService.Create(
                user.Id,
                session.Id,
                refreshTokenPair.PlainTextToken,
                challengeExpiresAtUtc);

            await refreshSessionRepository.AddAsync(session, cancellationToken);

            var challengeAudit = auditLogFactory.Create(
                AuditActionType.MfaLoginChallenged,
                AuditPayloadBuilder.Build(new
                {
                    @event = "mfa_login_challenged",
                    userId = user.Id,
                    sessionId = session.Id
                }),
                user.Id);

            await auditLogRepository.AddAsync(challengeAudit, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<LoginResponse>.Success(
                new LoginResponse(
                    new UserDto(
                        user.Id,
                        user.Email,
                        user.EmailVerified,
                        user.IsActive),
                    null,
                    new MfaChallengeResponse(
                        challengeToken,
                        challengeExpiresAtUtc),
                    true));
        }

        var permissions = await roleRepository.GetPermissionCodesByUserIdAsync(user.Id, cancellationToken);

        var accessToken = await tokenGenerator.GenerateAccessTokenAsync(
            user.Id,
            user.Email,
            permissions,
            session.Id,
            cancellationToken);

        await refreshSessionRepository.AddAsync(session, cancellationToken);

        var successAudit = auditLogFactory.Create(
            AuditActionType.LoginSucceeded,
            AuditPayloadBuilder.Build(new
            {
                @event = "login_succeeded",
                email = user.Email,
                sessionId = session.Id
            }),
            user.Id);

        await auditLogRepository.AddAsync(successAudit, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var response = new LoginResponse(
            new UserDto(user.Id, user.Email, user.EmailVerified, user.IsActive),
            new AuthTokensDto(
                accessToken.AccessToken,
                accessToken.AccessTokenExpiresAtUtc,
                refreshTokenPair.PlainTextToken,
                refreshTokenExpiresAtUtc),
                null,
                false
            );

        return Result<LoginResponse>.Success(response);
    }

    private async Task WriteFailedLoginAuditAsync(
        string normalizedEmail,
        CancellationToken cancellationToken)
    {
        var auditLog = auditLogFactory.Create(
            AuditActionType.LoginFailed,
            AuditPayloadBuilder.Build(new
            {
                @event = "login_failed",
                normalizedEmail
            }));

        await auditLogRepository.AddAsync(auditLog, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}