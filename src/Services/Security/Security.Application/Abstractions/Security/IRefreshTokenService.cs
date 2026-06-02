namespace Security.Application.Abstractions.Security;

public interface IRefreshTokenService
{
    (string PlainText, string Hash) Create();
    string Hash(string token);
}

//Kullanılmıyor iptal edilecek...