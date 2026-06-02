using MediatR;
using Security.Application.Common.Results;

namespace Security.Application.Auth.Mfa.RecoveryCodes;

public sealed record RegenerateRecoveryCodesCommand(
    Guid UserId,
    string TotpCode) : IRequest<Result<RegenerateRecoveryCodesResponse>>;