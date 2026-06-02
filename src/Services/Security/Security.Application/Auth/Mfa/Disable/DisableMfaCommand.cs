using MediatR;
using Security.Application.Common.Results;

namespace Security.Application.Auth.Mfa.Disable;

public sealed record DisableMfaCommand(
    Guid UserId,
    string? TotpCode,
    string? RecoveryCode) : IRequest<Result>;