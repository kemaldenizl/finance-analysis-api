namespace Security.API.Contracts.Auth;

public sealed record RegenerateRecoveryCodesRequest(
    string TotpCode);