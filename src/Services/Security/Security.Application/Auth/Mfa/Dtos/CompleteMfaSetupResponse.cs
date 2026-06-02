namespace Security.Application.Auth.Mfa.Dtos;

public sealed record CompleteMfaSetupResponse(IReadOnlyCollection<string> RecoveryCodes);