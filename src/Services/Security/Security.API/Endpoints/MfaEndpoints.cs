using MediatR;
using Security.API.Common;
using Security.API.Common.Auth;
using Security.API.Common.ErrorMapping;
using Security.API.Contracts.Auth;
using Security.Application.Auth.Mfa.BeginSetup;
using Security.Application.Auth.Mfa.CompleteSetup;
using Security.Application.Auth.Mfa.VerifyLogin;
using Security.Infrastructure.RateLimiting;

namespace Security.API.Endpoints;

public static class MfaEndpoints
{
    public static IEndpointRouteBuilder MapMfaEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/mfa")
            .WithTags(ApiTags.Auth);

        group.MapPost("/setup/begin", BeginSetupAsync)
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitPolicyNames.VerifyEmail)
            .WithName("BeginMfaSetup")
            .WithSummary("Starts TOTP MFA setup.")
            .Produces<BeginMfaSetupResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithOpenApi();

        group.MapPost("/setup/complete", CompleteSetupAsync)
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitPolicyNames.VerifyEmail)
            .WithName("CompleteMfaSetup")
            .WithSummary("Completes TOTP MFA setup.")
            .Accepts<CompleteMfaSetupRequest>("application/json")
            .Produces<CompleteMfaSetupResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithOpenApi();

        group.MapPost("/login/complete", CompleteLoginAsync)
            .RequireRateLimiting(RateLimitPolicyNames.Login)
            .WithName("CompleteMfaLogin")
            .WithSummary("Completes MFA login challenge.")
            .Accepts<CompleteMfaLoginRequest>("application/json")
            .Produces<LoginResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithOpenApi();

        return app;
    }

    private static async Task<IResult> BeginSetupAsync(
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var currentUser = httpContext.User.ToCurrentUser();

        var command = new BeginMfaSetupCommand(
            currentUser.UserId,
            currentUser.Email);

        var result = await sender.Send(command, cancellationToken);

        return httpContext.ToApiResult(result);
    }

    private static async Task<IResult> CompleteSetupAsync(
        CompleteMfaSetupRequest request,
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var currentUser = httpContext.User.ToCurrentUser();

        var command = new CompleteMfaSetupCommand(
            currentUser.UserId,
            request.Code);

        var result = await sender.Send(command, cancellationToken);

        return httpContext.ToApiResult(result);
    }

    private static async Task<IResult> CompleteLoginAsync(
        CompleteMfaLoginRequest request,
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new CompleteMfaLoginCommand(
            request.ChallengeToken,
            request.TotpCode,
            request.RecoveryCode);

        var result = await sender.Send(command, cancellationToken);

        return httpContext.ToApiResult(result);
    }
}