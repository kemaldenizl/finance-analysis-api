using Security.Domain.Mfa;

namespace Security.Application.Abstractions.Persistence;

public interface IMfaMethodRepository
{
    Task<MfaMethod?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task AddAsync(MfaMethod method, CancellationToken cancellationToken = default);
}