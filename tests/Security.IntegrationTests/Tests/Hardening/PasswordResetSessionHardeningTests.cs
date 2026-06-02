using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Security.Domain.Tokens;
using Security.Application.Abstractions.Security;
using Security.Infrastructure.Persistence;
using Security.Infrastructure.Security;
using Security.IntegrationTests.Contracts.Auth;
using Security.IntegrationTests.Fixtures;
using Security.IntegrationTests.Infrastructure;
using Xunit;

namespace Security.IntegrationTests.Tests.Hardening;

[Collection(IntegrationTestCollection.Name)]
public sealed class PasswordResetSessionHardeningTests
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _clientA;
    private readonly HttpClient _clientB;

    public PasswordResetSessionHardeningTests(IntegrationTestFixture fixture)
    {
        _factory = new CustomWebApplicationFactory(fixture);

        _clientA = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        _clientB = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task PasswordReset_Should_Revoke_All_Sessions_And_Invalidate_All_User_AccessTokens()
    {
        var email = $"pwd-hardening-{Guid.NewGuid():N}@example.com";
        const string oldPassword = "Str0ng!Password123";
        const string newPassword = "An0ther!StrongPassword456";

        await _clientA.RegisterAsync(email, oldPassword);

        var tokensA = await _clientA.LoginAndSetBearerAsync(email, oldPassword);
        var tokensB = await _clientB.LoginAndSetBearerAsync(email, oldPassword);

        await _clientA.AssertMeOkAsync();
        await _clientB.AssertMeOkAsync();

        string plainResetToken;

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<SecurityDbContext>();
            var tokenGenerator = scope.ServiceProvider.GetRequiredService<IPasswordResetTokenGenerator>();

            var user = dbContext.Users.Single(x => x.Email == email);

            var tokenPair = tokenGenerator.Generate();
            plainResetToken = tokenPair.PlainTextToken;

            dbContext.PasswordResetTokens.Add(new PasswordResetToken(
                Guid.NewGuid(),
                user.Id,
                tokenPair.HashedToken,
                DateTime.UtcNow.AddMinutes(30),
                DateTime.UtcNow));

            await dbContext.SaveChangesAsync();
        }

        var resetResponse = await _clientA.PostAsJsonAsync(
            "/api/auth/reset-password",
            new ResetPasswordRequest(plainResetToken, newPassword));

        resetResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        await _clientA.AssertMeUnauthorizedAsync();
        await _clientB.AssertMeUnauthorizedAsync();

        _clientA.ClearBearerToken();
        _clientB.ClearBearerToken();

        var refreshAResponse = await _clientA.PostAsJsonAsync(
            "/api/auth/refresh",
            new RefreshTokenRequest(tokensA.RefreshToken));

        refreshAResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var refreshBResponse = await _clientB.PostAsJsonAsync(
            "/api/auth/refresh",
            new RefreshTokenRequest(tokensB.RefreshToken));

        refreshBResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var oldPasswordLoginResponse = await _clientA.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(email, oldPassword));

        oldPasswordLoginResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var newPasswordLoginResponse = await _clientA.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(email, newPassword));

        newPasswordLoginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}