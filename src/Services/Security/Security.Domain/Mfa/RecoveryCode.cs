using Security.Domain.Common;

namespace Security.Domain.Mfa;

public sealed class RecoveryCode
{
    public Guid Id { get; private set; }
    public Guid MfaMethodId { get; private set; }
    public string CodeHash { get; private set; } = default!;
    public bool Used { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UsedAtUtc { get; private set; }

    private RecoveryCode()
    {
    }

    public RecoveryCode(
        Guid id,
        Guid mfaMethodId,
        string codeHash,
        DateTime createdAtUtc)
    {
        Id = Guard.AgainstEmpty(id, nameof(id));
        MfaMethodId = Guard.AgainstEmpty(mfaMethodId, nameof(mfaMethodId));
        CodeHash = Guard.AgainstNullOrWhiteSpace(codeHash, nameof(codeHash));
        CreatedAtUtc = Guard.AgainstDefault(createdAtUtc, nameof(createdAtUtc));
    }

    public void MarkUsed(DateTime utcNow)
    {
        utcNow = Guard.AgainstDefault(utcNow, nameof(utcNow));

        Guard.Against(Used, "Recovery code is already used.");

        Used = true;
        UsedAtUtc = utcNow;
    }
}