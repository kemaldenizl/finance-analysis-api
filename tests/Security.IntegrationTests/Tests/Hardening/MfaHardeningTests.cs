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
public sealed class MfaHardeningTests
{
    private readonly HttpClient _client;

    public MfaHardeningTests(IntegrationTestFixture fixture)
    {
        var factory = new CustomWebApplicationFactory(fixture);

        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task DisableMfa_Should_Stop_Requiring_Mfa_On_Login()
    {
        var email = $"mfa-disable-{Guid.NewGuid():N}@example.com";
        const string password = "Str0ng!Password123";

        await _client.RegisterAsync(email, password);

        var tokens = await _client.LoginAndSetBearerAsync(email, password);

        var beginResponse = await _client.PostAsync("/api/mfa/setup/begin", null);
        beginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var beginPayload = await beginResponse.Content.ReadAsync<BeginMfaSetupResponse>();
        beginPayload.Should().NotBeNull();

        var setupCode = MfaTestCodeGenerator.GenerateCode(beginPayload!.ManualEntryKey);

        var completeResponse = await _client.PostAsJsonAsync(
            "/api/mfa/setup/complete",
            new CompleteMfaSetupRequest(setupCode));

        completeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var completePayload = await completeResponse.Content.ReadAsync<CompleteMfaSetupResponse>();
        completePayload.Should().NotBeNull();
        completePayload!.RecoveryCodes.Should().NotBeEmpty();

        _client.ClearBearerToken();

        var mfaLoginResponse = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(email, password));

        mfaLoginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var mfaLogin = await mfaLoginResponse.Content.ReadAsync<LoginResponse>();
        mfaLogin.Should().NotBeNull();
        mfaLogin!.RequiresMfa.Should().BeTrue();
        mfaLogin.Tokens.Should().BeNull();
        mfaLogin.MfaChallenge.Should().NotBeNull();

        _client.SetBearerToken(tokens.AccessToken);

        var disableCode = MfaTestCodeGenerator.GenerateCode(beginPayload.ManualEntryKey);

        var disableResponse = await _client.PostAsJsonAsync(
            "/api/mfa/disable",
            new DisableMfaRequest(disableCode, null));

        disableResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        _client.ClearBearerToken();

        var normalLoginResponse = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(email, password));

        normalLoginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var normalLogin = await normalLoginResponse.Content.ReadAsync<LoginResponse>();
        normalLogin.Should().NotBeNull();
        normalLogin!.RequiresMfa.Should().BeFalse();
        normalLogin.Tokens.Should().NotBeNull();
        normalLogin.MfaChallenge.Should().BeNull();
    }

    [Fact]
    public async Task RegenerateRecoveryCodes_Should_Invalidates_Old_RecoveryCodes_And_Enable_New_Ones()
    {
        var email = $"mfa-recovery-{Guid.NewGuid():N}@example.com";
        const string password = "Str0ng!Password123";

        await _client.RegisterAsync(email, password);

        var tokens = await _client.LoginAndSetBearerAsync(email, password);

        var beginResponse = await _client.PostAsync("/api/mfa/setup/begin", null);
        beginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var beginPayload = await beginResponse.Content.ReadAsync<BeginMfaSetupResponse>();
        beginPayload.Should().NotBeNull();

        var setupCode = MfaTestCodeGenerator.GenerateCode(beginPayload!.ManualEntryKey);

        var completeResponse = await _client.PostAsJsonAsync(
            "/api/mfa/setup/complete",
            new CompleteMfaSetupRequest(setupCode));

        completeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var completePayload = await completeResponse.Content.ReadAsync<CompleteMfaSetupResponse>();
        completePayload.Should().NotBeNull();
        completePayload!.RecoveryCodes.Should().NotBeEmpty();

        var oldRecoveryCode = completePayload.RecoveryCodes.First();

        var regenerateCode = MfaTestCodeGenerator.GenerateCode(beginPayload.ManualEntryKey);

        _client.SetBearerToken(tokens.AccessToken);

        var regenerateResponse = await _client.PostAsJsonAsync(
            "/api/mfa/recovery-codes/regenerate",
            new RegenerateRecoveryCodesRequest(regenerateCode));

        regenerateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var regeneratePayload = await regenerateResponse.Content.ReadAsync<RegenerateRecoveryCodesResponse>();
        regeneratePayload.Should().NotBeNull();
        regeneratePayload!.RecoveryCodes.Should().NotBeEmpty();

        var newRecoveryCode = regeneratePayload.RecoveryCodes.First();
        newRecoveryCode.Should().NotBe(oldRecoveryCode);

        _client.ClearBearerToken();

        var loginResponse = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(email, password));

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginPayload = await loginResponse.Content.ReadAsync<LoginResponse>();
        loginPayload.Should().NotBeNull();
        loginPayload!.RequiresMfa.Should().BeTrue();
        loginPayload.MfaChallenge.Should().NotBeNull();

        var oldRecoveryLoginResponse = await _client.PostAsJsonAsync(
            "/api/mfa/login/complete",
            new CompleteMfaLoginRequest(
                loginPayload.MfaChallenge!.ChallengeToken,
                null,
                oldRecoveryCode));

        oldRecoveryLoginResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var secondLoginResponse = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(email, password));

        secondLoginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var secondLoginPayload = await secondLoginResponse.Content.ReadAsync<LoginResponse>();
        secondLoginPayload.Should().NotBeNull();
        secondLoginPayload!.RequiresMfa.Should().BeTrue();
        secondLoginPayload.MfaChallenge.Should().NotBeNull();

        var newRecoveryLoginResponse = await _client.PostAsJsonAsync(
            "/api/mfa/login/complete",
            new CompleteMfaLoginRequest(
                secondLoginPayload.MfaChallenge!.ChallengeToken,
                null,
                newRecoveryCode));

        newRecoveryLoginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var finalLoginPayload = await newRecoveryLoginResponse.Content.ReadAsync<LoginResponse>();
        finalLoginPayload.Should().NotBeNull();
        finalLoginPayload!.RequiresMfa.Should().BeFalse();
        finalLoginPayload.Tokens.Should().NotBeNull();
    }

    [Fact]
    public async Task MfaLogin_Should_Return_Tokens_When_Totp_Code_Is_Valid()
    {
        var email = $"mfa-login-{Guid.NewGuid():N}@example.com";
        const string password = "Str0ng!Password123";

        await _client.RegisterAsync(email, password);

        _ = await _client.LoginAndSetBearerAsync(email, password);

        var beginResponse = await _client.PostAsync("/api/mfa/setup/begin", null);
        beginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var beginPayload = await beginResponse.Content.ReadAsync<BeginMfaSetupResponse>();
        beginPayload.Should().NotBeNull();

        var setupCode = MfaTestCodeGenerator.GenerateCode(beginPayload!.ManualEntryKey);

        var completeResponse = await _client.PostAsJsonAsync(
            "/api/mfa/setup/complete",
            new CompleteMfaSetupRequest(setupCode));

        completeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        _client.ClearBearerToken();

        var loginResponse = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(email, password));

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginPayload = await loginResponse.Content.ReadAsync<LoginResponse>();
        loginPayload.Should().NotBeNull();
        loginPayload!.RequiresMfa.Should().BeTrue();
        loginPayload.Tokens.Should().BeNull();
        loginPayload.MfaChallenge.Should().NotBeNull();

        var loginCode = MfaTestCodeGenerator.GenerateCode(beginPayload.ManualEntryKey);

        var completeLoginResponse = await _client.PostAsJsonAsync(
            "/api/mfa/login/complete",
            new CompleteMfaLoginRequest(
                loginPayload.MfaChallenge!.ChallengeToken,
                loginCode,
                null));

        completeLoginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var completedLogin = await completeLoginResponse.Content.ReadAsync<LoginResponse>();
        completedLogin.Should().NotBeNull();
        completedLogin!.RequiresMfa.Should().BeFalse();
        completedLogin.Tokens.Should().NotBeNull();
        completedLogin.Tokens!.AccessToken.Should().NotBeNullOrWhiteSpace();
        completedLogin.Tokens.RefreshToken.Should().NotBeNullOrWhiteSpace();
    }
}