using Microsoft.EntityFrameworkCore;
using Security.Application.Abstractions.Persistence;
using Security.Domain.Tokens;

namespace Security.Infrastructure.Persistence.Repositories;

public sealed class PasswordResetTokenRepository(SecurityDbContext dbContext) : IPasswordResetTokenRepository
{
    public Task AddAsync(PasswordResetToken token, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(token);

        return dbContext.PasswordResetTokens.AddAsync(token, cancellationToken).AsTask();
    }

    public async Task<PasswordResetToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return await dbContext.PasswordResetTokens.FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);
    }
}