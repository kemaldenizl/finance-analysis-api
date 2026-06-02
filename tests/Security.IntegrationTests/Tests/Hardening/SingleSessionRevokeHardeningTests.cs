using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Security.IntegrationTests.Contracts.Auth;
using Security.IntegrationTests.Contracts.Sessions;
using Security.IntegrationTests.Fixtures;
using Security.IntegrationTests.Infrastructure;
using Xunit;

namespace Security.IntegrationTests.Tests.Hardening;

[Collection(IntegrationTestCollection.Name)]
public sealed class SingleSessionRevokeHardeningTests
{
    private readonly HttpClient _clientA;
    private readonly HttpClient _clientB;

    public SingleSessionRevokeHardeningTests(IntegrationTestFixture fixture)
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
    public async Task RevokeSession_Should_Invalidate_Target_Session_AccessToken_But_Keep_Current_Session_Alive()
    {
        var email = $"session-hardening-{Guid.NewGuid():N}@example.com";
        const string password = "Str0ng!Password123";

        await _clientA.RegisterAsync(email, password);

        var tokensA = await _clientA.LoginAndSetBearerAsync(email, password);
        var tokensB = await _clientB.LoginAndSetBearerAsync(email, password);

        await _clientA.AssertMeOkAsync();
        await _clientB.AssertMeOkAsync();

        var sessionsResponse = await _clientA.GetAsync("/api/sessions");
        sessionsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var sessions = await sessionsResponse.Content.ReadAsync<SessionResponse[]>();
        sessions.Should().NotBeNull();
        sessions!.Length.Should().BeGreaterThanOrEqualTo(2);

        var targetSession = sessions.First(x => !x.IsCurrent);

        var revokeResponse = await _clientA.DeleteAsync($"/api/sessions/{targetSession.SessionId}");
        revokeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        await _clientA.AssertMeOkAsync();
        await _clientB.AssertMeUnauthorizedAsync();

        _clientB.ClearBearerToken();

        var refreshBResponse = await _clientB.PostAsJsonAsync(
            "/api/auth/refresh",
            new RefreshTokenRequest(tokensB.RefreshToken));

        refreshBResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        _clientA.DefaultRequestHeaders.Authorization.Should().NotBeNull();
        tokensA.AccessToken.Should().NotBeNullOrWhiteSpace();
    }
}