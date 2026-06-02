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
using Security.Domain.Mfa;

namespace Security.Application.Auth.Mfa.RecoveryCodes;

public sealed class RegenerateRecoveryCodesCommandHandler(
    IMfaMethodRepository mfaMethodRepository,
    IAuditLogRepository auditLogRepository,
    IAuditLogFactory auditLogFactory,
    ITotpSecretProtector totpSecretProtector,
    ITotpService totpService,
    IRecoveryCodeService recoveryCodeService,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RegenerateRecoveryCodesCommand, Result<RegenerateRecoveryCodesResponse>>
{
    public async Task<Result<RegenerateRecoveryCodesResponse>> Handle(
        RegenerateRecoveryCodesCommand request,
        CancellationToken cancellationToken)
    {
        var method = await mfaMethodRepository.GetByUserIdAsync(
            request.UserId,
            cancellationToken);

        if (method is null || !method.IsEnabled || !method.IsVerified)
        {
            return Result<RegenerateRecoveryCodesResponse>.Failure(AuthErrors.MfaNotEnabled);
        }

        var secret = totpSecretProtector.Unprotect(method.SecretEncrypted);
        var verified = totpService.VerifyCode(secret, request.TotpCode);

        if (!verified)
        {
            return Result<RegenerateRecoveryCodesResponse>.Failure(AuthErrors.InvalidMfaCode);
        }

        var utcNow = dateTimeProvider.UtcNow;
        var recoveryCodes = recoveryCodeService.GenerateCodes(10);

        method.ClearRecoveryCodes();

        foreach (var code in recoveryCodes)
        {
            method.AddRecoveryCode(new RecoveryCode(
                Guid.NewGuid(),
                method.Id,
                recoveryCodeService.Hash(code),
                utcNow));
        }

        var auditLog = auditLogFactory.Create(
            AuditActionType.RecoveryCodesRegenerated,
            AuditPayloadBuilder.Build(new
            {
                @event = "recovery_codes_regenerated",
                userId = request.UserId,
                count = recoveryCodes.Count
            }),
            request.UserId);

        await auditLogRepository.AddAsync(auditLog, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<RegenerateRecoveryCodesResponse>.Success(
            new RegenerateRecoveryCodesResponse(recoveryCodes));
    }
}