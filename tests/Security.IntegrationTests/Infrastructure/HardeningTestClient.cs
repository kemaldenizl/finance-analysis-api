using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Security.IntegrationTests.Contracts.Auth;

namespace Security.IntegrationTests.Infrastructure;

public static class HardeningTestClient
{
    public static async Task RegisterAsync(
        this HttpClient client,
        string email,
        string password)
    {
        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(email, password));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    public static async Task<LoginResponse> LoginAsync(
        this HttpClient client,
        string email,
        string password)
    {
        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(email, password));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadAsync<LoginResponse>();
        payload.Should().NotBeNull();

        return payload!;
    }

    public static async Task<AuthTokensResponse> LoginAndSetBearerAsync(
        this HttpClient client,
        string email,
        string password)
    {
        var login = await client.LoginAsync(email, password);

        login.RequiresMfa.Should().BeFalse();
        login.Tokens.Should().NotBeNull();

        client.SetBearerToken(login.Tokens!.AccessToken);

        return login.Tokens;
    }

    public static async Task AssertMeOkAsync(this HttpClient client)
    {
        var response = await client.GetAsync("/api/users/me");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    public static async Task AssertMeUnauthorizedAsync(this HttpClient client)
    {
        var response = await client.GetAsync("/api/users/me");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}