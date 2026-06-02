namespace Security.IntegrationTests.Contracts.Auth;

public sealed record CompleteMfaSetupResponse(
    IReadOnlyCollection<string> RecoveryCodes);