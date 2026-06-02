namespace Security.API.Contracts.Auth;

public sealed record CompleteMfaSetupResponse(IReadOnlyCollection<string> RecoveryCodes);