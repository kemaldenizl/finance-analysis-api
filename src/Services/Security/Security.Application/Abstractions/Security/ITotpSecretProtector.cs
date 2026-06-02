namespace Security.Application.Abstractions.Security;

public interface ITotpSecretProtector
{
    string Protect(string plainSecret);
    string Unprotect(string protectedSecret);
    string Hash(string plainSecret);
}