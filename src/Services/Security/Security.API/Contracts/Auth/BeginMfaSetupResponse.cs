namespace Security.API.Contracts.Auth;

public sealed record BeginMfaSetupResponse(string ManualEntryKey, string OtpAuthUri);