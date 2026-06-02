using Microsoft.EntityFrameworkCore;
using Security.Application.Abstractions.Persistence;
using Security.Domain.Mfa;

namespace Security.Infrastructure.Persistence.Repositories;

public sealed class MfaMethodRepository(SecurityDbContext dbContext) : IMfaMethodRepository
{
    public async Task<MfaMethod?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<MfaMethod>()
            .Include(x => x.RecoveryCodes)
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
    }

    public Task AddAsync(MfaMethod method, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(method);
        return dbContext.Set<MfaMethod>().AddAsync(method, cancellationToken).AsTask();
    }
}