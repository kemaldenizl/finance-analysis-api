using Security.API.Contracts.Auth;
using Security.API.ProblemDetails;
using Security.Application.Common.Results;
using AppLoginResponse = Security.Application.Auth.Login.LoginResponse;
using AppRefreshTokenResponse = Security.Application.Auth.Refresh.RefreshTokenResponse;
using AppRegisterResponse = Security.Application.Auth.Register.RegisterResponse;
using AppForgotPasswordResponse = Security.Application.Auth.PasswordReset.Dtos.ForgotPasswordResponse;
using AppResendVerificationResponse = Security.Application.Auth.EmailVerification.Dtos.ResendVerificationResponse;
using AppBeginMfaSetupResponse = Security.Application.Auth.Mfa.Dtos.BeginMfaSetupResponse;
using AppCompleteMfaSetupResponse = Security.Application.Auth.Mfa.Dtos.CompleteMfaSetupResponse;
using System.Text.Json;

namespace Security.API.Common.ErrorMapping;

public static class ApplicationResultMapper
{
    public static IResult ToApiResult(this HttpContext httpContext, Result<AppResendVerificationResponse> result)
    {
        if (result.IsSuccess)
        {
            var response = new Contracts.Auth.ResendVerificationResponse(result.Value.Message);
            return Results.Accepted(value: response);
        }

        return MapFailure(httpContext, result);
    }

    public static IResult ToApiResult(this HttpContext httpContext, Result<AppForgotPasswordResponse> result)
    {
        if (result.IsSuccess)
        {
            var response = new Contracts.Auth.ForgotPasswordResponse(result.Value.Message);
            return Results.Accepted(value: response);
        }

        return MapFailure(httpContext, result);
    }

    public static IResult ToApiResult(this HttpContext httpContext, Result result)
    {
        if (result.IsSuccess)
        {
            return Results.NoContent();
        }

        return MapFailure(httpContext, result);
    }

    public static IResult ToApiResult(this HttpContext httpContext, Result<AppRegisterResponse> result)
    {
        if (result.IsSuccess)
        {
            var response = new RegisterResponse(
                new UserResponse(
                    result.Value.User.Id,
                    result.Value.User.Email,
                    result.Value.User.EmailVerified,
                    result.Value.User.IsActive));

            return Results.Created($"/api/users/{response.User.Id}", response);
        }

        return MapFailure(httpContext, result);
    }

    public static IResult ToApiResult(this HttpContext httpContext, Result<AppLoginResponse> result)
    {
        if (result.IsSuccess)
        {
            var response = new LoginResponse(
                new UserResponse(
                    result.Value.User.Id,
                    result.Value.User.Email,
                    result.Value.User.EmailVerified,
                    result.Value.User.IsActive),
                result.Value.Tokens is null
                    ? null
                    : new AuthTokensResponse(
                        result.Value.Tokens.AccessToken,
                        result.Value.Tokens.AccessTokenExpiresAtUtc,
                        result.Value.Tokens.RefreshToken,
                        result.Value.Tokens.RefreshTokenExpiresAtUtc),
                result.Value.MfaChallenge is null
                    ? null
                    : new Contracts.Auth.MfaChallengeResponse(
                        result.Value.MfaChallenge.ChallengeToken,
                        result.Value.MfaChallenge.ExpiresAtUtc),
                result.Value.RequiresMfa);

            return Results.Ok(response);
        }

        return MapFailure(httpContext, result);
    }

    public static IResult ToApiResult(this HttpContext httpContext, Result<AppRefreshTokenResponse> result)
    {
        if (result.IsSuccess)
        {
            var response = new RefreshTokenResponse(
                new AuthTokensResponse(
                    result.Value.Tokens.AccessToken,
                    result.Value.Tokens.AccessTokenExpiresAtUtc,
                    result.Value.Tokens.RefreshToken,
                    result.Value.Tokens.RefreshTokenExpiresAtUtc));

            return Results.Ok(response);
        }

        return MapFailure(httpContext, result);
    }
    
    public static IResult ToApiResult(this HttpContext httpContext, Result<AppBeginMfaSetupResponse> result)
    {
        if (result.IsSuccess)
        {
            return Results.Ok(new Contracts.Auth.BeginMfaSetupResponse(
            result.Value.ManualEntryKey,
            result.Value.OtpAuthUri));
        }

        return MapFailure(httpContext, result);
    }

    public static IResult ToApiResult(this HttpContext httpContext, Result<AppCompleteMfaSetupResponse> result)
    {
        if (result.IsSuccess)
        {
            return Results.Ok(new Contracts.Auth.CompleteMfaSetupResponse(result.Value.RecoveryCodes));
        }

        return MapFailure(httpContext, result);
    }

    private static IResult MapFailure(Result result)
    {
        throw new InvalidOperationException("HttpContext is required for problem details mapping.");
    }

    private static IResult MapFailure<T>(HttpContext httpContext, Result<T> result)
    {
        return MapFailure(httpContext, (Result)result);
    }

    private static IResult MapFailure(HttpContext httpContext, Result result)
    {
        var errorCode = result.Error.Code;

        return errorCode switch
        {
            "validation.invalid" => httpContext.ToValidationProblemResult(httpContext.CreateValidationProblemDetails(ParseValidationErrors(result.Error.Description))),

            "auth.invalid_credentials" => httpContext.ToProblemResult(
                httpContext.CreateProblemDetails(
                    StatusCodes.Status401Unauthorized,
                    "Authentication failed",
                    result.Error.Description)),

            "auth.invalid_refresh_token" => httpContext.ToProblemResult(
                httpContext.CreateProblemDetails(
                    StatusCodes.Status401Unauthorized,
                    "Invalid refresh token",
                    result.Error.Description)),

            "auth.expired_refresh_token" => httpContext.ToProblemResult(
                httpContext.CreateProblemDetails(
                    StatusCodes.Status401Unauthorized,
                    "Expired refresh token",
                    result.Error.Description)),

            "auth.revoked_refresh_token" => httpContext.ToProblemResult(
                httpContext.CreateProblemDetails(
                    StatusCodes.Status401Unauthorized,
                    "Revoked refresh token",
                    result.Error.Description)),

            "auth.consumed_refresh_token" => httpContext.ToProblemResult(
                httpContext.CreateProblemDetails(
                    StatusCodes.Status401Unauthorized,
                    "Consumed refresh token",
                    result.Error.Description)),

            "auth.session_revoked" => httpContext.ToProblemResult(
                httpContext.CreateProblemDetails(
                    StatusCodes.Status401Unauthorized,
                    "Session revoked",
                    result.Error.Description)),

            "auth.refresh_token_reuse_detected" => httpContext.ToProblemResult(
                httpContext.CreateProblemDetails(
                    StatusCodes.Status401Unauthorized,
                    "Refresh token reuse detected",
                    result.Error.Description)),

            "auth.invalid_session" => httpContext.ToProblemResult(
                httpContext.CreateProblemDetails(
                    StatusCodes.Status400BadRequest,
                    "Invalid session",
                    result.Error.Description)),

            "auth.session_not_found" => httpContext.ToProblemResult(
                httpContext.CreateProblemDetails(
                    StatusCodes.Status404NotFound,
                    "Session not found",
                    result.Error.Description)),

            "auth.user_inactive" => httpContext.ToProblemResult(
                httpContext.CreateProblemDetails(
                    StatusCodes.Status403Forbidden,
                    "User inactive",
                    result.Error.Description)),

            "auth.email_not_verified" => httpContext.ToProblemResult(
                httpContext.CreateProblemDetails(
                    StatusCodes.Status403Forbidden,
                    "Email is not verified",
                    result.Error.Description)),

            "auth.user_already_exists" => httpContext.ToProblemResult(
                httpContext.CreateProblemDetails(
                    StatusCodes.Status409Conflict,
                    "Conflict",
                    result.Error.Description)),
            
            "auth.invalid_password_reset_token" => httpContext.ToProblemResult(
                httpContext.CreateProblemDetails(
                    StatusCodes.Status400BadRequest,
                    "Invalid password reset token",
                    result.Error.Description)),

            "auth.expired_password_reset_token" => httpContext.ToProblemResult(
                httpContext.CreateProblemDetails(
                    StatusCodes.Status400BadRequest,
                    "Expired password reset token",
                    result.Error.Description)),

            "auth.used_password_reset_token" => httpContext.ToProblemResult(
                httpContext.CreateProblemDetails(
                    StatusCodes.Status400BadRequest,
                    "Used password reset token",
                    result.Error.Description)),
            
            "auth.invalid_email_verification_token" => httpContext.ToProblemResult(
                httpContext.CreateProblemDetails(
                    StatusCodes.Status400BadRequest,
                    "Invalid email verification token",
                    result.Error.Description)),

            "auth.expired_email_verification_token" => httpContext.ToProblemResult(
                httpContext.CreateProblemDetails(
                    StatusCodes.Status400BadRequest,
                    "Expired email verification token",
                    result.Error.Description)),

            "auth.used_email_verification_token" => httpContext.ToProblemResult(
                httpContext.CreateProblemDetails(
                    StatusCodes.Status400BadRequest,
                    "Used email verification token",
                    result.Error.Description)),

            "auth.email_already_verified" => httpContext.ToProblemResult(
                httpContext.CreateProblemDetails(
                    StatusCodes.Status400BadRequest,
                    "Email already verified",
                    result.Error.Description)),

            "auth.mfa_not_initialized" => httpContext.ToProblemResult(
                httpContext.CreateProblemDetails(
                    StatusCodes.Status400BadRequest,
                    "MFA not initialized",
                    result.Error.Description)),

            "auth.invalid_mfa_code" => httpContext.ToProblemResult(
                httpContext.CreateProblemDetails(
                    StatusCodes.Status400BadRequest,
                    "Invalid MFA code",
                    result.Error.Description)),

            "auth.invalid_mfa_challenge" => httpContext.ToProblemResult(
                httpContext.CreateProblemDetails(
                    StatusCodes.Status401Unauthorized,
                    "Invalid MFA challenge",
                    result.Error.Description)),

            _ => httpContext.ToProblemResult(
                httpContext.CreateProblemDetails(
                    StatusCodes.Status400BadRequest,
                    "Application error",
                    result.Error.Description))
        };
    }

    private static IDictionary<string, string[]> ParseValidationErrors(string description)
    {
        try
        {
            var parsed = JsonSerializer.Deserialize<Dictionary<string, string[]>>(description);
            if (parsed is not null && parsed.Count > 0)
                return parsed;
        }
        catch
        {
            // ignored intentionally
        }

        return new Dictionary<string, string[]>
        {
            ["request"] = [description]
        };
    }
}