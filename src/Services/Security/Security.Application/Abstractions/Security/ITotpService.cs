namespace Security.Application.Abstractions.Security;

public interface ITotpService
{
    string GenerateSecret();

    string BuildOtpAuthUri(string issuer, string accountName, string secret);

    bool VerifyCode(string secret, string code);
}