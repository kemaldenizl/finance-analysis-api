using MediatR;
using Security.Application.Common.Results;

namespace Security.Application.Auth.EmailVerification.VerifyEmail;

public sealed record VerifyEmailCommand( string Token) : IRequest<Result>;