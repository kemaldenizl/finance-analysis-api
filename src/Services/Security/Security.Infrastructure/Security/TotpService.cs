using OtpNet;
using Security.Application.Abstractions.Security;

namespace Security.Infrastructure.Security;

public sealed class TotpService : ITotpService
{
    public string GenerateSecret()
    {
        var key = KeyGeneration.GenerateRandomKey(20);
        return Base32Encoding.ToString(key);
    }

    public string BuildOtpAuthUri(string issuer, string accountName, string secret)
    {
        var uri = new OtpUri(
            OtpType.Totp,
            secret,
            accountName,
            issuer);

        return uri.ToString();
    }

    public bool VerifyCode(string secret, string code)
    {
        var totp = new Totp(Base32Encoding.ToBytes(secret));
        return totp.VerifyTotp(code, out _, VerificationWindow.RfcSpecifiedNetworkDelay);
    }
}