using Security.Application.Auth;

namespace Security.Application.Abstractions.Security;

public interface IJwtIssuer
{
    Task<TokenResponse> IssueForUserAsync(Guid userId, CancellationToken cancellationToken = default);
}

//Kullanılmıyor iptal edilecek...
