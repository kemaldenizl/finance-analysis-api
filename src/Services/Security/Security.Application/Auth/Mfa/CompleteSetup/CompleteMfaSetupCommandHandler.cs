using MediatR;
using Security.Application.Abstractions.Auditing;
using Security.Application.Abstractions.Persistence;
using Security.Application.Abstractions.Security;
using Security.Application.Abstractions.Time;
using Security.Application.Abstractions.UnitOfWork;
using Security.Application.Auth.Mfa.Dtos;
using Security.Application.Common.Auditing;
using Security.Application.Common.Errors;
using Security.Application.Common.Results;
using Security.Domain.Auditing;
using Security.Domain.Mfa;

namespace Security.Application.Auth.Mfa.CompleteSetup;

public sealed class CompleteMfaSetupCommandHandler(
    IMfaMethodRepository mfaMethodRepository,
    IAuditLogRepository auditLogRepository,
    IAuditLogFactory auditLogFactory,
    ITotpSecretProtector totpSecretProtector,
    ITotpService totpService,
    IRecoveryCodeService recoveryCodeService,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CompleteMfaSetupCommand, Result<CompleteMfaSetupResponse>>
{
    public async Task<Result<CompleteMfaSetupResponse>> Handle(
        CompleteMfaSetupCommand request,
        CancellationToken cancellationToken)
    {
        var method = await mfaMethodRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (method is null)
        {
            return Result<CompleteMfaSetupResponse>.Failure(
                new("auth.mfa_not_initialized", "MFA setup has not been started."));
        }

        var secret = totpSecretProtector.Unprotect(method.SecretEncrypted);

        var valid = totpService.VerifyCode(secret, request.Code);
        if (!valid)
        {
            return Result<CompleteMfaSetupResponse>.Failure(
                new("auth.invalid_mfa_code", "The MFA code is invalid."));
        }

        var utcNow = dateTimeProvider.UtcNow;
        method.VerifyAndEnable(utcNow);

        var recoveryCodes = recoveryCodeService.GenerateCodes(10);

        foreach (var code in recoveryCodes)
        {
            method.AddRecoveryCode(new RecoveryCode(
                Guid.NewGuid(),
                method.Id,
                recoveryCodeService.Hash(code),
                utcNow));
        }

        var auditLog = auditLogFactory.Create(
            AuditActionType.MfaEnabled,
            AuditPayloadBuilder.Build(new
            {
                @event = "mfa_enabled",
                userId = request.UserId,
                recoveryCodeCount = recoveryCodes.Count
            }),
            request.UserId);

        await auditLogRepository.AddAsync(auditLog, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<CompleteMfaSetupResponse>.Success(
            new CompleteMfaSetupResponse(recoveryCodes));
    }
}