using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Security.Application.Abstractions.Security;

namespace Security.Infrastructure.Security;

public sealed class MfaChallengeTokenService(IDataProtectionProvider dataProtectionProvider) : IMfaChallengeTokenService
{
    private readonly IDataProtector _protector =
        dataProtectionProvider.CreateProtector("security-service.mfa.challenge-token");

    public string Create(Guid userId, Guid sessionId, string refreshToken, DateTime expiresAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);
        var payload = new MfaChallengeTokenPayload(userId, sessionId, refreshToken, expiresAtUtc);
        var json = JsonSerializer.Serialize(payload);
        return _protector.Protect(json);
    }

    public MfaChallengeTokenPayload? Validate(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;
        try
        {
            var json = _protector.Unprotect(token);
            return JsonSerializer.Deserialize<MfaChallengeTokenPayload>(json);
        }
        catch
        {
            return null;
        }
    }
}