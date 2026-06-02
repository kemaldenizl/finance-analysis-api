using System.Security.Cryptography;
using System.Text;
using Security.Application.Abstractions.Security;

namespace Security.Infrastructure.Security;

public sealed class RecoveryCodeService : IRecoveryCodeService
{
    public IReadOnlyCollection<string> GenerateCodes(int count = 10)
    {
        var codes = new List<string>(count);

        for (var i = 0; i < count; i++)
        {
            Span<byte> bytes = stackalloc byte[8];
            RandomNumberGenerator.Fill(bytes);

            var code = Convert.ToHexString(bytes)[..10];
            codes.Add(code);
        }

        return codes;
    }

    public string Hash(string code)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(code));
        return Convert.ToHexString(bytes);
    }
}