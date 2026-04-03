using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Security.Domain.Tokens;
using Security.Infrastructure.Persistence;
using Security.Infrastructure.Security;
using Security.IntegrationTests.Contracts.Auth;
using Security.IntegrationTests.Fixtures;
using Security.IntegrationTests.Infrastructure;
using Xunit;
using Security.Application.Abstractions.Security;

namespace Security.IntegrationTests.Tests.Auth;

[Collection(IntegrationTestCollection.Name)]
public sealed class EmailVerificationTests
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public EmailVerificationTests(IntegrationTestFixture fixture)
    {
        _factory = new CustomWebApplicationFactory(fixture);
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task ResendVerification_Should_Return_Accepted_For_Existing_User()
    {
        var email = $"verify-{Guid.NewGuid():N}@example.com";
        const string password = "Str0ng!Password123";

        await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(email, password));

        var response = await _client.PostAsJsonAsync("/api/auth/resend-verification",
            new ResendVerificationRequest(email));

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var payload = await response.Content.ReadAsync<ResendVerificationResponse>();
        payload.Should().NotBeNull();
        payload!.Message.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ResendVerification_Should_Return_Accepted_For_NonExisting_User()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/resend-verification",
            new ResendVerificationRequest($"missing-{Guid.NewGuid():N}@example.com"));

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var payload = await response.Content.ReadAsync<ResendVerificationResponse>();
        payload.Should().NotBeNull();
        payload!.Message.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task VerifyEmail_Should_Mark_User_As_Verified_With_Valid_Token()
    {
        var email = $"verify-complete-{Guid.NewGuid():N}@example.com";
        const string password = "Str0ng!Password123";

        await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(email, password));

        string plainToken;

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<SecurityDbContext>();
            var tokenGenerator = scope.ServiceProvider.GetRequiredService<IEmailVerificationTokenGenerator>();

            var user = dbContext.Users.Single(x => x.Email == email);

            var tokenPair = tokenGenerator.Generate();
            plainToken = tokenPair.PlainTextToken;

            dbContext.EmailVerificationTokens.Add(new EmailVerificationToken(
                Guid.NewGuid(),
                user.Id,
                tokenPair.HashedToken,
                DateTime.UtcNow.AddHours(24),
                DateTime.UtcNow));

            await dbContext.SaveChangesAsync();
        }

        var verifyResponse = await _client.PostAsJsonAsync("/api/auth/verify-email",
            new VerifyEmailRequest(plainToken));

        verifyResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<SecurityDbContext>();
            var user = dbContext.Users.Single(x => x.Email == email);

            user.EmailVerified.Should().BeTrue();
        }
    }
}