namespace Security.IntegrationTests.Contracts.Auth;

public sealed record CompleteMfaSetupRequest(
    string Code);