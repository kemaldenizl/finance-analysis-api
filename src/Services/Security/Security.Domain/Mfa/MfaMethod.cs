using Security.Domain.Abstractions;
using Security.Domain.Common;

namespace Security.Domain.Mfa;

public sealed class MfaMethod : AggregateRoot
{
    private readonly List<RecoveryCode> _recoveryCodes = [];

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public MfaMethodType Type { get; private set; }
    public string SecretHash { get; private set; } = default!;
    public string SecretEncrypted { get; private set; } = default!;
    public bool IsVerified { get; private set; }
    public bool IsEnabled { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? VerifiedAtUtc { get; private set; }
    public DateTime? DisabledAtUtc { get; private set; }

    public IReadOnlyCollection<RecoveryCode> RecoveryCodes => _recoveryCodes.AsReadOnly();

    private MfaMethod()
    {
    }

    public MfaMethod(
        Guid id,
        Guid userId,
        MfaMethodType type,
        string secretHash,
        string secretEncrypted,
        DateTime createdAtUtc)
    {
        Id = Guard.AgainstEmpty(id, nameof(id));
        UserId = Guard.AgainstEmpty(userId, nameof(userId));
        Type = type;
        SecretHash = Guard.AgainstNullOrWhiteSpace(secretHash, nameof(secretHash));
        SecretEncrypted = Guard.AgainstNullOrWhiteSpace(secretEncrypted, nameof(secretEncrypted));
        CreatedAtUtc = Guard.AgainstDefault(createdAtUtc, nameof(createdAtUtc));
        IsEnabled = false;
        IsVerified = false;
    }

    public void VerifyAndEnable(DateTime utcNow)
    {
        utcNow = Guard.AgainstDefault(utcNow, nameof(utcNow));

        IsVerified = true;
        IsEnabled = true;
        VerifiedAtUtc = utcNow;
        DisabledAtUtc = null;
    }

    public void Disable(DateTime utcNow)
    {
        utcNow = Guard.AgainstDefault(utcNow, nameof(utcNow));

        IsEnabled = false;
        DisabledAtUtc = utcNow;
    }

    public void AddRecoveryCode(RecoveryCode code)
    {
        ArgumentNullException.ThrowIfNull(code);
        _recoveryCodes.Add(code);
    }

    public RecoveryCode? GetUsableRecoveryCodeByHash(string codeHash)
    {
        codeHash = Guard.AgainstNullOrWhiteSpace(codeHash, nameof(codeHash));
        return _recoveryCodes.FirstOrDefault(x => x.CodeHash == codeHash && !x.Used);
    }
}