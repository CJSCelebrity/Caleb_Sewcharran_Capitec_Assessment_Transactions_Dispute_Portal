using CapitecDisputes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CapitecDisputes.Infrastructure.Persistence.Configurations;

internal sealed class DisputeConfiguration : IEntityTypeConfiguration<Dispute>
{
    public void Configure(EntityTypeBuilder<Dispute> builder)
    {
        builder.ToTable("disputes");
        builder.HasKey(dispute => dispute.Id);
        builder.Property(dispute => dispute.Id).ValueGeneratedNever();

        builder.Property(dispute => dispute.CustomerId).IsRequired();
        builder.Property(dispute => dispute.TransactionAmount).HasPrecision(18, 2);
        builder.Property(dispute => dispute.TransactionCurrency).HasMaxLength(3).IsRequired();
        builder.Property(dispute => dispute.MerchantName).HasMaxLength(200).IsRequired();
        builder.Property(dispute => dispute.Reason).HasMaxLength(500).IsRequired();
        builder.Property(dispute => dispute.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.HasIndex(dispute => dispute.CustomerId);
        builder.HasIndex(dispute => dispute.Status);
        builder.HasIndex(dispute => dispute.TransactionId);

        builder.Metadata
            .FindNavigation(nameof(Dispute.StatusChanges))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(dispute => dispute.StatusChanges)
            .WithOne()
            .HasForeignKey(change => change.DisputeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
