using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Security.Domain.Mfa;

namespace Security.Infrastructure.Persistence.Configurations.Mfa;

public sealed class MfaMethodConfiguration : IEntityTypeConfiguration<MfaMethod>
{
    public void Configure(EntityTypeBuilder<MfaMethod> builder)
    {
        builder.ToTable("mfa_methods");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.Type).HasConversion<int>().IsRequired();
        builder.Property(x => x.SecretHash).HasMaxLength(128).IsRequired();
        builder.Property(x => x.SecretEncrypted).IsRequired();
        builder.Property(x => x.IsVerified).IsRequired();
        builder.Property(x => x.IsEnabled).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.VerifiedAtUtc);
        builder.Property(x => x.DisabledAtUtc);

        builder.HasIndex(x => x.UserId)
            .IsUnique()
            .HasDatabaseName("ix_mfa_methods_user_id");

        builder.Ignore(x => x.DomainEvents);

        builder.HasMany<RecoveryCode>("_recoveryCodes")
            .WithOne()
            .HasForeignKey(x => x.MfaMethodId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.RecoveryCodes)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}