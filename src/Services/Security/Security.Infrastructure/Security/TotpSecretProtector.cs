using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Security.Application.Abstractions.Security;

namespace Security.Infrastructure.Security;

public sealed class TotpSecretProtector(IDataProtectionProvider dataProtectionProvider) : ITotpSecretProtector
{
    private readonly IDataProtector _protector =
        dataProtectionProvider.CreateProtector("security-service.mfa.totp-secret");

    public string Protect(string plainSecret)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plainSecret);
        return _protector.Protect(plainSecret);
    }

    public string Unprotect(string protectedSecret)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(protectedSecret);
        return _protector.Unprotect(protectedSecret);
    }

    public string Hash(string plainSecret)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(plainSecret));
        return Convert.ToHexString(bytes);
    }
}