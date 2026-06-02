namespace Security.Application.Abstractions.Security;

public interface IPasswordService
{
    string Hash(string password);
    bool Verify(string hashedPassword, string providedPassword);
}

//Kullanılmıyor iptal edilecek...