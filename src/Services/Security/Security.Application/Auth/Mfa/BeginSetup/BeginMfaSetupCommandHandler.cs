using MediatR;
using Security.Application.Abstractions.Auditing;
using Security.Application.Abstractions.Persistence;
using Security.Application.Abstractions.Security;
using Security.Application.Abstractions.Time;
using Security.Application.Abstractions.UnitOfWork;
using Security.Application.Auth.Mfa.Dtos;
using Security.Application.Common.Auditing;
using Security.Application.Common.Results;
using Security.Domain.Auditing;
using Security.Domain.Mfa;

namespace Security.Application.Auth.Mfa.BeginSetup;

public sealed class BeginMfaSetupCommandHandler(
    IMfaMethodRepository mfaMethodRepository,
    IAuditLogRepository auditLogRepository,
    IAuditLogFactory auditLogFactory,
    ITotpService totpService,
    ITotpSecretProtector totpSecretProtector,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
    : IRequestHandler<BeginMfaSetupCommand, Result<BeginMfaSetupResponse>>
{
    private const string Issuer = "SecurityService";

    public async Task<Result<BeginMfaSetupResponse>> Handle(
        BeginMfaSetupCommand request,
        CancellationToken cancellationToken)
    {
        var existing = await mfaMethodRepository.GetByUserIdAsync(request.UserId, cancellationToken);

        var secret = totpService.GenerateSecret();
        var encryptedSecret = totpSecretProtector.Protect(secret);
        var hashedSecret = totpSecretProtector.Hash(secret);

        var utcNow = dateTimeProvider.UtcNow;

        if (existing is null)
        {
            existing = new MfaMethod(
                Guid.NewGuid(),
                request.UserId,
                MfaMethodType.Totp,
                hashedSecret,
                encryptedSecret,
                utcNow);

            await mfaMethodRepository.AddAsync(existing, cancellationToken);
        }
        else
        {
            existing.ResetPendingSecret(hashedSecret, encryptedSecret);
        }

        var otpAuthUri = totpService.BuildOtpAuthUri(Issuer, request.Email, secret);

        var auditLog = auditLogFactory.Create(
            AuditActionType.MfaSetupStarted,
            AuditPayloadBuilder.Build(new
            {
                @event = "mfa_setup_started",
                userId = request.UserId,
                type = "totp"
            }),
            request.UserId);

        await auditLogRepository.AddAsync(auditLog, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<BeginMfaSetupResponse>.Success(
            new BeginMfaSetupResponse(secret, otpAuthUri));
    }
}