using Microsoft.EntityFrameworkCore;
using Security.Application.Abstractions.Persistence;
using Security.Domain.Tokens;

namespace Security.Infrastructure.Persistence.Repositories;

public sealed class EmailVerificationTokenRepository(SecurityDbContext dbContext) : IEmailVerificationTokenRepository
{
    public Task AddAsync(EmailVerificationToken token, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(token);

        return dbContext.EmailVerificationTokens.AddAsync(token, cancellationToken).AsTask();
    }

    public async Task<EmailVerificationToken?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.EmailVerificationTokens
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);
    }
}