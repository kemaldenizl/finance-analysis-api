using MediatR;
using Security.Application.Auth.PasswordReset.Dtos;
using Security.Application.Common.Results;

namespace Security.Application.Auth.PasswordReset.ForgotPassword;

public sealed record ForgotPasswordCommand(string Email) : IRequest<Result<ForgotPasswordResponse>>;