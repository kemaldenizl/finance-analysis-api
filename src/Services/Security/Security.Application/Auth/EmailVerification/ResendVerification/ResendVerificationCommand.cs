using MediatR;
using Security.Application.Auth.EmailVerification.Dtos;
using Security.Application.Common.Results;

namespace Security.Application.Auth.EmailVerification.ResendVerification;

public sealed record ResendVerificationCommand(string Email) : IRequest<Result<ResendVerificationResponse>>;