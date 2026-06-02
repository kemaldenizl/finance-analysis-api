using MediatR;
using Security.Application.Common.Results;

namespace Security.Application.Auth.PasswordReset.ResetPassword;

public sealed record ResetPasswordCommand(string Token, string NewPassword) : IRequest<Result>;
    