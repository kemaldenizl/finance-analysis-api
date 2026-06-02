using MediatR;
using Security.Application.Auth.Mfa.Dtos;
using Security.Application.Common.Results;

namespace Security.Application.Auth.Mfa.CompleteSetup;

public sealed record CompleteMfaSetupCommand(Guid UserId, string Code) : IRequest<Result<CompleteMfaSetupResponse>>;