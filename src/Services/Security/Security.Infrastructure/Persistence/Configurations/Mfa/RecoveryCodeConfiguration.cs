using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Security.Domain.Mfa;

namespace Security.Infrastructure.Persistence.Configurations.Mfa;

public sealed class RecoveryCodeConfiguration : IEntityTypeConfiguration<RecoveryCode>
{
    public void Configure(EntityTypeBuilder<RecoveryCode> builder)
    {
        builder.ToTable("recovery_codes");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.MfaMethodId).IsRequired();
        builder.Property(x => x.CodeHash).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Used).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UsedAtUtc);

        builder.HasIndex(x => x.MfaMethodId)
            .HasDatabaseName("ix_recovery_codes_mfa_method_id");

        builder.HasIndex(x => x.CodeHash)
            .HasDatabaseName("ix_recovery_codes_code_hash");
    }
}