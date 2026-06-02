using MediatR;
using Security.API.Abstractions;
using Security.API.Common;
using Security.API.Common.Auth;
using Security.API.Contracts.Auth;
using Security.API.Common.ErrorMapping;
using Security.API.Contracts.Errors;
using Security.Application.Auth.Login;
using Security.Application.Auth.Register;
using Security.Application.Auth.Refresh;
using Security.Application.Auth.Logout;
using Security.Application.Common.Results;
using Security.Infrastructure.RateLimiting;
using Security.Application.Auth.PasswordReset.ForgotPassword;
using Security.Application.Auth.PasswordReset.ResetPassword;
using Security.Application.Auth.EmailVerification.ResendVerification;
using Security.Application.Auth.EmailVerification.VerifyEmail;

namespace Security.API.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags(ApiTags.Auth);

        group.MapPost("/register", RegisterAsync)
            .RequireRateLimiting(RateLimitPolicyNames.Register)
            .WithName("Register")
            .WithSummary("Registers a new user.")
            .WithDescription("Creates a new user account.")
            .Accepts<RegisterRequest>("application/json")
            .Produces<Security.API.Contracts.Auth.RegisterResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .WithOpenApi();

        group.MapPost("/login", LoginAsync)
            .RequireRateLimiting(RateLimitPolicyNames.Login)
            .WithName("Login")
            .WithSummary("Authenticates a user.")
            .WithDescription("Authenticates a user with email and password and returns access and refresh tokens.")
            .Accepts<LoginRequest>("application/json")
            .Produces<Contracts.Auth.LoginResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .WithOpenApi();

        group.MapPost("/refresh", RefreshAsync)
            .RequireRateLimiting(RateLimitPolicyNames.Refresh)
            .WithName("RefreshToken")
            .WithSummary("Refreshes access and refresh tokens.")
            .WithDescription("Consumes a valid refresh token, rotates it, and returns a new access token and refresh token.")
            .Accepts<RefreshTokenRequest>("application/json")
            .Produces<Contracts.Auth.RefreshTokenResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .WithOpenApi();

        group.MapPost("/logout", LogoutAsync)
            .RequireRateLimiting(RateLimitPolicyNames.Logout)
            .RequireAuthorization()
            .WithName("Logout")
            .WithSummary("Logs out the current session.")
            .WithDescription("Revokes the current authenticated session.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .WithOpenApi();

        group.MapPost("/logout-all", LogoutAllAsync)
            .RequireRateLimiting(RateLimitPolicyNames.Logout)
            .RequireAuthorization()
            .WithName("LogoutAll")
            .WithSummary("Logs out all sessions.")
            .WithDescription("Revokes all sessions belonging to the current authenticated user.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .WithOpenApi();

        group.MapPost("/forgot-password", ForgotPasswordAsync)
            .RequireRateLimiting(RateLimitPolicyNames.ForgotPassword)
            .WithName("ForgotPassword")
            .WithSummary("Starts the password reset flow.")
            .WithDescription("Generates a password reset token if the account exists.")
            .Accepts<ForgotPasswordRequest>("application/json")
            .Produces<ForgotPasswordResponse>(StatusCodes.Status202Accepted)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .WithOpenApi();

        group.MapPost("/reset-password", ResetPasswordAsync)
            .RequireRateLimiting(RateLimitPolicyNames.ResetPassword)
            .WithName("ResetPassword")
            .WithSummary("Completes the password reset flow.")
            .WithDescription("Validates the password reset token and changes the user's password.")
            .Accepts<ResetPasswordRequest>("application/json")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .WithOpenApi();

        group.MapPost("/verify-email", VerifyEmailAsync)
            .RequireRateLimiting(RateLimitPolicyNames.VerifyEmail)
            .WithName("VerifyEmail")
            .WithSummary("Verifies the user's email address.")
            .WithDescription("Validates an email verification token and marks the email as verified.")
            .Accepts<VerifyEmailRequest>("application/json")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .WithOpenApi();

        group.MapPost("/resend-verification", ResendVerificationAsync)
            .RequireRateLimiting(RateLimitPolicyNames.ResendVerification)
            .WithName("ResendVerification")
            .WithSummary("Resends the email verification flow.")
            .WithDescription("Generates a new email verification token if the account exists and is not verified.")
            .Accepts<ResendVerificationRequest>("application/json")
            .Produces<ResendVerificationResponse>(StatusCodes.Status202Accepted)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .WithOpenApi();

        return app;
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequest request,
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new RegisterCommand(
            request.Email,
            request.Password);

        var result = await sender.Send(command, cancellationToken);

        return httpContext.ToApiResult(result);
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new LoginCommand(
            request.Email,
            request.Password
        );

        var result = await sender.Send(command, cancellationToken);

        return httpContext.ToApiResult(result);
    }

    private static async Task<IResult> RefreshAsync(
        RefreshTokenRequest request,
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new RefreshTokenCommand(
            request.RefreshToken
        );

        var result = await sender.Send(command, cancellationToken);

        return httpContext.ToApiResult(result);
    }

    private static async Task<IResult> LogoutAsync(
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var currentUser = httpContext.User.ToCurrentUser();
        var currentAccessToken = httpContext.User.ToCurrentAccessToken();

        if (currentUser.SessionId is null || string.IsNullOrWhiteSpace(currentUser.AccessTokenJti))
        {
            return Results.Problem(
                title: "Invalid session context",
                detail: "The current access token does not contain a valid session context.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var command = new LogoutCommand(
            currentUser.UserId,
            currentUser.SessionId.Value,
            currentUser.AccessTokenJti,
            currentAccessToken.ExpiresAtUtc
        );

        var result = await sender.Send(command, cancellationToken);

        return httpContext.ToApiResult(result);
    }

    private static async Task<IResult> LogoutAllAsync(
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var currentUser = httpContext.User.ToCurrentUser();
        var currentAccessToken = httpContext.User.ToCurrentAccessToken();

        if (string.IsNullOrWhiteSpace(currentUser.AccessTokenJti))
        {
            return Results.Problem(
                title: "Invalid token context",
                detail: "The current access token does not contain a valid JWT identifier.",
                statusCode: StatusCodes.Status400BadRequest
            );
        }

        var command = new LogoutAllCommand(currentUser.UserId, currentUser.AccessTokenJti, currentAccessToken.ExpiresAtUtc);
        var result = await sender.Send(command, cancellationToken);

        return httpContext.ToApiResult(result);
    }

    private static async Task<IResult> ForgotPasswordAsync(ForgotPasswordRequest request, HttpContext httpContext, ISender sender, CancellationToken cancellationToken) 
    {
        var command = new ForgotPasswordCommand(request.Email);

        var result = await sender.Send(command, cancellationToken);

        return httpContext.ToApiResult(result);
    }

    private static async Task<IResult> ResetPasswordAsync(ResetPasswordRequest request, HttpContext httpContext, ISender sender, CancellationToken cancellationToken)
    {
        var command = new ResetPasswordCommand(request.Token, request.NewPassword);
        var result = await sender.Send(command, cancellationToken);

        return httpContext.ToApiResult(result);
    }

    private static async Task<IResult> VerifyEmailAsync(VerifyEmailRequest request, HttpContext httpContext, ISender sender, CancellationToken cancellationToken)
    {
        var command = new VerifyEmailCommand(request.Token);
        var result = await sender.Send(command, cancellationToken);

        return httpContext.ToApiResult(result);
    }

    private static async Task<IResult> ResendVerificationAsync(ResendVerificationRequest request, HttpContext httpContext, ISender sender, CancellationToken cancellationToken)
    {
        var command = new ResendVerificationCommand(request.Email);
        var result = await sender.Send(command, cancellationToken);

        return httpContext.ToApiResult(result);
    }
}