namespace Security.Application.Abstractions.Security;

public interface IEmailVerificationTokenGenerator
{
    (string PlainTextToken, string HashedToken) Generate();
    string Hash(string plainTextToken);
}