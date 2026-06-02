using System.Security.Cryptography;
using System.Text;
using Security.Application.Abstractions.Security;

namespace Security.Infrastructure.Security;

public sealed class EmailVerificationTokenGenerator : IEmailVerificationTokenGenerator
{
    public (string PlainTextToken, string HashedToken) Generate()
    {
        Span<byte> bytes = stackalloc byte[64];
        RandomNumberGenerator.Fill(bytes);

        var plainTextToken = Convert.ToBase64String(bytes);
        var hashedToken = Hash(plainTextToken);

        return (plainTextToken, hashedToken);
    }

    public string Hash(string plainTextToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plainTextToken);

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(plainTextToken));
        return Convert.ToHexString(bytes);
    }
}