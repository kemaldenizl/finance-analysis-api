namespace Security.IntegrationTests.Contracts.Auth;

public sealed record BeginMfaSetupResponse(
    string ManualEntryKey,
    string OtpAuthUri);