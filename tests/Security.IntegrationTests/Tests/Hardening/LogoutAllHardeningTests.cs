using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Security.IntegrationTests.Contracts.Auth;
using Security.IntegrationTests.Fixtures;
using Security.IntegrationTests.Infrastructure;
using Xunit;

namespace Security.IntegrationTests.Tests.Hardening;

[Collection(IntegrationTestCollection.Name)]
public sealed class LogoutAllHardeningTests
{
    private readonly HttpClient _clientA;
    private readonly HttpClient _clientB;

    public LogoutAllHardeningTests(IntegrationTestFixture fixture)
    {
        var factory = new CustomWebApplicationFactory(fixture);

        _clientA = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        _clientB = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task LogoutAll_Should_Invalidate_Other_Client_AccessToken_And_RefreshToken()
    {
        var email = $"logout-all-hardening-{Guid.NewGuid():N}@example.com";
        const string password = "Str0ng!Password123";

        await _clientA.RegisterAsync(email, password);

        _ = await _clientA.LoginAndSetBearerAsync(email, password);
        var tokensB = await _clientB.LoginAndSetBearerAsync(email, password);

        await _clientA.AssertMeOkAsync();
        await _clientB.AssertMeOkAsync();

        var logoutAllResponse = await _clientA.PostAsync("/api/auth/logout-all", null);
        logoutAllResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        await _clientA.AssertMeUnauthorizedAsync();
        await _clientB.AssertMeUnauthorizedAsync();

        _clientB.ClearBearerToken();

        var refreshBResponse = await _clientB.PostAsJsonAsync(
            "/api/auth/refresh",
            new RefreshTokenRequest(tokensB.RefreshToken));

        refreshBResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}