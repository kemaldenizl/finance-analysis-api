using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Security.Application.Abstractions.Authentication;
using Security.Application.Auth.Dtos;
using Security.Application.Common.Security;

namespace Security.Infrastructure.Security.Jwt;

public sealed class JwtTokenGenerator(IOptions<JwtOptions> options) : ITokenGenerator
{
    private readonly JwtOptions _options = options.Value;

    public Task<AccessTokenDto> GenerateAccessTokenAsync(
        Guid userId,
        string email,
        IReadOnlyCollection<string> permissions,
        Guid? sessionId = null,
        CancellationToken cancellationToken = default)
    {
        ValidateOptions(_options);

        var now = DateTime.UtcNow;
        var expiresAtUtc = now.AddMinutes(_options.AccessTokenLifetimeMinutes);

        var claims = new List<Claim>
        {
            new(CustomClaimTypes.Subject, userId.ToString()),
            new(CustomClaimTypes.Email, email),
            new(CustomClaimTypes.JwtId, Guid.NewGuid().ToString("N")),
            new(CustomClaimTypes.IssuedAt, new DateTimeOffset(now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        if (sessionId.HasValue)
        {
            claims.Add(new Claim(CustomClaimTypes.SessionId, sessionId.Value.ToString()));
        }

        claims.AddRange(permissions.Select(permission => new Claim(CustomClaimTypes.Permission, permission)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Expires = expiresAtUtc,
            SigningCredentials = credentials
        };

        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateToken(tokenDescriptor);
        var accessToken = handler.WriteToken(token);

        return Task.FromResult(new AccessTokenDto(accessToken, expiresAtUtc));
    }

    private static void ValidateOptions(JwtOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Issuer))
            throw new InvalidOperationException("Jwt:Issuer is missing.");

        if (string.IsNullOrWhiteSpace(options.Audience))
            throw new InvalidOperationException("Jwt:Audience is missing.");

        if (string.IsNullOrWhiteSpace(options.SigningKey))
            throw new InvalidOperationException("Jwt:SigningKey is missing.");

        if (options.SigningKey.Length < 32)
            throw new InvalidOperationException("Jwt:SigningKey must be at least 32 characters.");
    }
}