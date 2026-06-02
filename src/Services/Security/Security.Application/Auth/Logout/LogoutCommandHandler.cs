using MediatR;
using Microsoft.Extensions.Options;
using Security.Application.Abstractions.Auditing;
using Security.Application.Abstractions.Persistence;
using Security.Application.Abstractions.Security;
using Security.Application.Abstractions.Time;
using Security.Application.Abstractions.UnitOfWork;
using Security.Application.Common.Auditing;
using Security.Application.Common.Errors;
using Security.Application.Common.Results;
using Security.Domain.Auditing;

namespace Security.Application.Auth.Logout;

public sealed class LogoutCommandHandler(
    IRefreshSessionRepository refreshSessionRepository,
    IAuditLogRepository auditLogRepository,
    IAuditLogFactory auditLogFactory,
    IAccessTokenRevocationStore accessTokenRevocationStore,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork,
    IOptions<SecurityTokenInvalidationOptions> invalidationOptions)
    : IRequestHandler<LogoutCommand, Result>
{
    private readonly SecurityTokenInvalidationOptions _invalidationOptions = invalidationOptions.Value;

    public async Task<Result> Handle(
        LogoutCommand request,
        CancellationToken cancellationToken)
    {
        var utcNow = dateTimeProvider.UtcNow;

        var session = await refreshSessionRepository.GetByIdAsync(
            request.SessionId,
            cancellationToken);

        if (session is null)
        {
            return Result.Failure(AuthErrors.SessionNotFound);
        }

        if (session.UserId != request.UserId)
        {
            return Result.Failure(AuthErrors.InvalidSession);
        }

        session.Revoke(utcNow);

        await accessTokenRevocationStore.RevokeAsync(
            request.AccessTokenJti,
            request.AccessTokenExpiresAtUtc,
            cancellationToken);

        await accessTokenRevocationStore.RevokeSessionAsync(
            session.Id,
            utcNow,
            utcNow.AddHours(_invalidationOptions.SessionInvalidationRetentionHours),
            cancellationToken);

        var auditLog = auditLogFactory.Create(
            AuditActionType.LogoutCurrentSession,
            AuditPayloadBuilder.Build(new
            {
                @event = "logout_current_session",
                sessionId = request.SessionId,
                accessTokenJti = request.AccessTokenJti,
                sessionAccessTokensInvalidated = true
            }),
            request.UserId);

        await auditLogRepository.AddAsync(auditLog, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}