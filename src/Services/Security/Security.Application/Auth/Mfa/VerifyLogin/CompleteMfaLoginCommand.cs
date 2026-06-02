using MediatR;
using Security.Application.Auth.Login;
using Security.Application.Common.Results;

namespace Security.Application.Auth.Mfa.VerifyLogin;

public sealed record CompleteMfaLoginCommand(string ChallengeToken, string? TotpCode, string? RecoveryCode) : IRequest<Result<LoginResponse>>;