namespace Security.IntegrationTests.Contracts.Auth;

public sealed record ResetPasswordRequest(string Token, string NewPassword);
   
    