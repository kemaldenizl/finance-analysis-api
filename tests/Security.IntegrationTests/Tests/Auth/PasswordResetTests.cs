using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Security.IntegrationTests.Contracts.Auth;
using Security.IntegrationTests.Fixtures;
using Security.IntegrationTests.Infrastructure;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Security.Infrastructure.Persistence;
using Security.Domain.Tokens;
using Security.Infrastructure.Security;
using Security.Application.Abstractions.Security;

namespace Security.IntegrationTests.Tests.Auth;

[Collection(IntegrationTestCollection.Name)]
public sealed class PasswordResetTests
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public PasswordResetTests(IntegrationTestFixture fixture)
    {
        _factory = new CustomWebApplicationFactory(fixture);
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task ForgotPassword_Should_Return_Accepted_For_Existing_User()
    {
        var email = $"forgot-{Guid.NewGuid():N}@example.com";
        const string password = "Str0ng!Password123";

        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, password));
           
        var response = await _client.PostAsJsonAsync("/api/auth/forgot-password", new ForgotPasswordRequest(email));

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var payload = await response.Content.ReadAsync<ForgotPasswordResponse>();
        payload.Should().NotBeNull();
        payload!.Message.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ForgotPassword_Should_Return_Accepted_For_NonExisting_User()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/forgot-password", new ForgotPasswordRequest($"no-user-{Guid.NewGuid():N}@example.com"));
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var payload = await response.Content.ReadAsync<ForgotPasswordResponse>();
        payload.Should().NotBeNull();
        payload!.Message.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ResetPassword_Should_Change_User_Password_With_Valid_Token()
    {
        var email = $"reset-{Guid.NewGuid():N}@example.com";
        const string oldPassword = "Str0ng!Password123";
        const string newPassword = "An0ther!StrongPassword456";

        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, oldPassword));
        string plainToken;

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<SecurityDbContext>();
            var tokenGenerator = scope.ServiceProvider.GetRequiredService<IPasswordResetTokenGenerator>();

            var user = dbContext.Users.Single(x => x.Email == email);

            var tokenPair = tokenGenerator.Generate();
            plainToken = tokenPair.PlainTextToken;

            dbContext.PasswordResetTokens.Add(
                new PasswordResetToken(
                    Guid.NewGuid(),
                    user.Id,
                    tokenPair.HashedToken,
                    DateTime.UtcNow.AddMinutes(30),
                    DateTime.UtcNow
                )
            );

            await dbContext.SaveChangesAsync();
        }

        var resetResponse = await _client.PostAsJsonAsync("/api/auth/reset-password", new ResetPasswordRequest(plainToken, newPassword));
        resetResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var oldLoginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, oldPassword));
        oldLoginResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var newLoginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, newPassword));
        newLoginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}