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

namespace Security.Application.Auth.Mfa.Disable;

public sealed class DisableMfaCommandHandler(
    IMfaMethodRepository mfaMethodRepository,
    IAuditLogRepository auditLogRepository,
    IAuditLogFactory auditLogFactory,
    ITotpSecretProtector totpSecretProtector,
    ITotpService totpService,
    IRecoveryCodeService recoveryCodeService,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DisableMfaCommand, Result>
{
    public async Task<Result> Handle(
        DisableMfaCommand request,
        CancellationToken cancellationToken)
    {
        var method = await mfaMethodRepository.GetByUserIdAsync(
            request.UserId,
            cancellationToken);

        if (method is null || !method.IsEnabled || !method.IsVerified)
        {
            return Result.Failure(AuthErrors.MfaNotEnabled);
        }

        var utcNow = dateTimeProvider.UtcNow;

        var verified = false;
        var usedRecoveryCode = false;

        if (!string.IsNullOrWhiteSpace(request.TotpCode))
        {
            var secret = totpSecretProtector.Unprotect(method.SecretEncrypted);
            verified = totpService.VerifyCode(secret, request.TotpCode);
        }
        else if (!string.IsNullOrWhiteSpace(request.RecoveryCode))
        {
            var recoveryCode = method.GetUsableRecoveryCodeByHash(
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
            return Result.Failure(AuthErrors.InvalidMfaCode);
        }

        method.Disable(utcNow);
        method.ClearRecoveryCodes();

        var auditLog = auditLogFactory.Create(
            AuditActionType.MfaDisabled,
            AuditPayloadBuilder.Build(new
            {
                @event = "mfa_disabled",
                userId = request.UserId,
                usedRecoveryCode
            }),
            request.UserId);

        await auditLogRepository.AddAsync(auditLog, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}