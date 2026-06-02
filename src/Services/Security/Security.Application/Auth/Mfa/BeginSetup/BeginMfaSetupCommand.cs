using MediatR;
using Security.Application.Auth.Mfa.Dtos;
using Security.Application.Common.Results;

namespace Security.Application.Auth.Mfa.BeginSetup;

public sealed record BeginMfaSetupCommand(Guid UserId, string Email) : IRequest<Result<BeginMfaSetupResponse>>;