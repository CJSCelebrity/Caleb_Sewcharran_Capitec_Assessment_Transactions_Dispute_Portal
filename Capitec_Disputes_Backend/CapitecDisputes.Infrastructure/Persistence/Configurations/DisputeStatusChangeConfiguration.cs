using CapitecDisputes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CapitecDisputes.Infrastructure.Persistence.Configurations;

internal sealed class DisputeStatusChangeConfiguration : IEntityTypeConfiguration<DisputeStatusChange>
{
    public void Configure(EntityTypeBuilder<DisputeStatusChange> builder)
    {
        builder.ToTable("dispute_status_changes");
        builder.HasKey(change => change.Id);
        builder.Property(change => change.Id).ValueGeneratedNever();

        builder.Property(change => change.FromStatus).HasConversion<string>().HasMaxLength(20);
        builder.Property(change => change.ToStatus).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(change => change.ChangedBy).HasMaxLength(256).IsRequired();

        builder.HasIndex(change => change.DisputeId);
    }
}
