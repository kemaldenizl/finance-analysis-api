using Security.Application.Auth.Dtos;
using Security.Application.Auth.Mfa.Dtos;

namespace Security.Application.Auth.Login;

public sealed record LoginResponse(
    UserDto User,
    AuthTokensDto? Tokens,
    MfaChallengeResponse? MfaChallenge,
    bool RequiresMfa);