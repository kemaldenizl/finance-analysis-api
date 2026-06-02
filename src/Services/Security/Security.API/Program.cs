using Microsoft.EntityFrameworkCore;
using Security.API.Endpoints;
using Security.Application;
using Security.Infrastructure;
using Security.Infrastructure.Persistence;
using Security.Infrastructure.Persistence.Seed;
using Security.API.Extensions;
using Security.API.Middleware;
using Security.API.OpenApi;
using Security.API.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Security.API.ProblemDetails;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "Security.API")
        .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
        .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning);
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddRateLimitExt(builder.Configuration);

builder.Services.AddExceptionHandler<ProblemDetailsExceptionHandler>();
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions[ProblemDetailsDefaults.CorrelationIdExtensionKey] = context.HttpContext.TraceIdentifier;            
        context.ProblemDetails.Instance ??= context.HttpContext.Request.Path;

        if (string.IsNullOrWhiteSpace(context.ProblemDetails.Type) && context.ProblemDetails.Status.HasValue)
        {
            context.ProblemDetails.Type = ProblemDetailsDefaults.StatusType(context.ProblemDetails.Status.Value);        
        }
    };
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});

builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy("API is running"), tags: ["live"])
    .AddDbContextCheck<SecurityDbContext>(name: "postgresql", failureStatus: HealthStatus.Unhealthy, tags: ["ready", "db"])
    .AddRedis(
        builder.Configuration.GetConnectionString("Redis") ?? throw new InvalidOperationException("Connection string 'Redis' was not found."),
        name: "redis",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["ready", "cache"]
    );

var app = builder.Build();

app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("CorrelationId", httpContext.TraceIdentifier);
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
        diagnosticContext.Set("RemoteIpAddress", httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");
    };
});

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<LogEnrichmentMiddleware>();

app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<SecurityDbContext>();

    if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("IntegrationTests"))
    {
        await dbContext.Database.MigrateAsync();

        var seeder = scope.ServiceProvider.GetRequiredService<IdentitySeeder>();
        await seeder.SeedAsync();
    }
    else if (app.Environment.IsStaging())
    {
        await dbContext.Database.MigrateAsync();
    }
}

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("live"),
    ResponseWriter = HealthCheckResponseWriter.WriteResponseAsync
})
.WithTags("Health")
.WithSummary("Liveness probe.")
.WithDescription("Returns whether the API process is alive.")
.WithOpenApi();

app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready"),
    ResponseWriter = HealthCheckResponseWriter.WriteResponseAsync
})
.WithTags("Health")
.WithSummary("Readiness probe.")
.WithDescription("Returns whether the API and its critical dependencies are ready.")
.WithOpenApi();

app.MapGet("/", () => Results.Ok(new
{
    service = "Security.API",
    status = "running"
}))
.WithTags("System")
.WithOpenApi();

app.MapAuthEndpoints();
app.MapUserEndpoints();
app.MapSessionEndpoints();
app.MapTestEndpoints();
app.MapMfaEndpoints();

app.Run();

public partial class Program;