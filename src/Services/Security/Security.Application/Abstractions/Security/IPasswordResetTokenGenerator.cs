namespace Security.Application.Abstractions.Security;

public interface IPasswordResetTokenGenerator
{
    (string PlainTextToken, string HashedToken) Generate();

    string Hash(string plainTextToken);
}