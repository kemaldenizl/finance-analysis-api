namespace Security.Application.Auth.Mfa.Dtos;

public sealed record BeginMfaSetupResponse(string ManualEntryKey, string OtpAuthUri);