namespace Security.Application.Abstractions.Security;

public interface IRecoveryCodeService
{
    IReadOnlyCollection<string> GenerateCodes(int count = 10);

    string Hash(string code);
}