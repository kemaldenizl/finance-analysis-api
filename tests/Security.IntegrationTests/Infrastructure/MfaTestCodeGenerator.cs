using OtpNet;

namespace Security.IntegrationTests.Infrastructure;

public static class MfaTestCodeGenerator
{
    public static string GenerateCode(string manualEntryKey)
    {
        var secretBytes = Base32Encoding.ToBytes(manualEntryKey);
        var totp = new Totp(secretBytes);

        return totp.ComputeTotp();
    }
}