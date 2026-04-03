using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Security.Infrastructure.RateLimiting;
using Security.API.Abstractions;

namespace Security.API.Extensions;

public static class RateLimitExtension
{
    public static IServiceCollection AddRateLimitExt(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RateLimitOptions>(configuration.GetSection(RateLimitOptions.SectionName));
        var rateLimitOptions = configuration.GetSection(RateLimitOptions.SectionName).Get<RateLimitOptions>() ?? new RateLimitOptions();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = "application/problem+json";

                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter = Math.Ceiling(retryAfter.TotalSeconds).ToString(System.Globalization.CultureInfo.InvariantCulture);                      
                }

                var problem = new Microsoft.AspNetCore.Mvc.ProblemDetails
                {
                    Type = "https://httpstatuses.com/429",
                    Title = "Too Many Requests",
                    Status = StatusCodes.Status429TooManyRequests,
                    Detail = "Rate limit exceeded. Please try again later.",
                    Instance = context.HttpContext.Request.Path
                };

                problem.Extensions["correlationId"] = context.HttpContext.TraceIdentifier;

                await context.HttpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
            };

            options.AddPolicy(RateLimitPolicyNames.Register, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: RateLimitPartitionKeys.ByIp(httpContext, "register"),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = rateLimitOptions.Register.PermitLimit,
                        Window = TimeSpan.FromSeconds(rateLimitOptions.Register.WindowSeconds),
                        QueueLimit = rateLimitOptions.Register.QueueLimit,
                        AutoReplenishment = rateLimitOptions.Register.AutoReplenishment
                    }));

            options.AddPolicy(RateLimitPolicyNames.Login, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: RateLimitPartitionKeys.ByIp(httpContext, "login"),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = rateLimitOptions.Login.PermitLimit,
                        Window = TimeSpan.FromSeconds(rateLimitOptions.Login.WindowSeconds),
                        QueueLimit = rateLimitOptions.Login.QueueLimit,
                        AutoReplenishment = rateLimitOptions.Login.AutoReplenishment
                    }));

            options.AddPolicy(RateLimitPolicyNames.Refresh, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: RateLimitPartitionKeys.ByIp(httpContext, "refresh"),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = rateLimitOptions.Refresh.PermitLimit,
                        Window = TimeSpan.FromSeconds(rateLimitOptions.Refresh.WindowSeconds),
                        QueueLimit = rateLimitOptions.Refresh.QueueLimit,
                        AutoReplenishment = rateLimitOptions.Refresh.AutoReplenishment
                    }));

            options.AddPolicy(RateLimitPolicyNames.Logout, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: RateLimitPartitionKeys.ByAuthenticatedUserOrIp(httpContext, "logout"),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = rateLimitOptions.Logout.PermitLimit,
                        Window = TimeSpan.FromSeconds(rateLimitOptions.Logout.WindowSeconds),
                        QueueLimit = rateLimitOptions.Logout.QueueLimit,
                        AutoReplenishment = rateLimitOptions.Logout.AutoReplenishment
                    }));

            options.AddPolicy(RateLimitPolicyNames.Sessions, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: RateLimitPartitionKeys.ByAuthenticatedUserOrIp(httpContext, "sessions"),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = rateLimitOptions.Sessions.PermitLimit,
                        Window = TimeSpan.FromSeconds(rateLimitOptions.Sessions.WindowSeconds),
                        QueueLimit = rateLimitOptions.Sessions.QueueLimit,
                        AutoReplenishment = rateLimitOptions.Sessions.AutoReplenishment
                    }));

            options.AddPolicy(RateLimitPolicyNames.ForgotPassword, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: RateLimitPartitionKeys.ByIp(httpContext, "forgot-password"),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = rateLimitOptions.ForgotPassword.PermitLimit,
                        Window = TimeSpan.FromSeconds(rateLimitOptions.ForgotPassword.WindowSeconds),
                        QueueLimit = rateLimitOptions.ForgotPassword.QueueLimit,
                        AutoReplenishment = rateLimitOptions.ForgotPassword.AutoReplenishment
                    }));

            options.AddPolicy(RateLimitPolicyNames.ResetPassword, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: RateLimitPartitionKeys.ByIp(httpContext, "reset-password"),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = rateLimitOptions.ResetPassword.PermitLimit,
                        Window = TimeSpan.FromSeconds(rateLimitOptions.ResetPassword.WindowSeconds),
                        QueueLimit = rateLimitOptions.ResetPassword.QueueLimit,
                        AutoReplenishment = rateLimitOptions.ResetPassword.AutoReplenishment
                    }));
        });

        return services;
    }
}