using Microsoft.EntityFrameworkCore;
using Security.Domain.Authorization;
using Security.Domain.Auditing;
using Security.Domain.Sessions;
using Security.Domain.Tokens;
using Security.Domain.Users;
using Security.Domain.Mfa;

namespace Security.Infrastructure.Persistence;

public sealed class SecurityDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public DbSet<RefreshSession> RefreshSessions => Set<RefreshSession>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<EmailVerificationToken> EmailVerificationTokens => Set<EmailVerificationToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<MfaMethod> MfaMethods => Set<MfaMethod>();
    public DbSet<RecoveryCode> RecoveryCodes => Set<RecoveryCode>();
    
    public SecurityDbContext(DbContextOptions<SecurityDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.HasDefaultSchema("security");

        builder.ApplyConfigurationsFromAssembly(typeof(SecurityDbContext).Assembly);

        builder.UseOpenIddict();
    }
}