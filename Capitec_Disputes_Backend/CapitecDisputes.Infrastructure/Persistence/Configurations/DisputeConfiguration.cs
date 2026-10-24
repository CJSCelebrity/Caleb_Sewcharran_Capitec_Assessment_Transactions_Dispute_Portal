using CapitecDisputes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CapitecDisputes.Infrastructure.Persistence.Configurations;

internal sealed class DisputeConfiguration : IEntityTypeConfiguration<Dispute>
{
    public void Configure(EntityTypeBuilder<Dispute> builder)
    {
        builder.ToTable("disputes");
        builder.HasKey(dispute => dispute.Id);

        builder.Property(dispute => dispute.TransactionAmount).HasPrecision(18, 2);
        builder.Property(dispute => dispute.TransactionCurrency).HasMaxLength(3).IsRequired();
        builder.Property(dispute => dispute.MerchantName).HasMaxLength(200).IsRequired();
        builder.Property(dispute => dispute.Reason).HasMaxLength(500).IsRequired();
        builder.Property(dispute => dispute.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.HasIndex(dispute => dispute.Status);
        builder.HasIndex(dispute => dispute.TransactionId);

        builder.HasData(
            new
            {
                Id = Guid.Parse("0199c0de-0001-7000-8000-000000000001"),
                TransactionId = Guid.Parse("0199c0de-1001-7000-8000-000000000001"),
                TransactionAmount = 1499.99m,
                TransactionCurrency = "ZAR",
                TransactionDate = new DateTimeOffset(2026, 10, 12, 9, 14, 0, TimeSpan.Zero),
                MerchantName = "Takealot",
                Reason = "Charged twice for a single order.",
                Status = DisputeStatus.Initiated,
                CreatedAt = new DateTimeOffset(2026, 10, 13, 7, 30, 0, TimeSpan.Zero),
                ResolvedAt = (DateTimeOffset?)null
            },
            new
            {
                Id = Guid.Parse("0199c0de-0001-7000-8000-000000000002"),
                TransactionId = Guid.Parse("0199c0de-1001-7000-8000-000000000002"),
                TransactionAmount = 320.00m,
                TransactionCurrency = "ZAR",
                TransactionDate = new DateTimeOffset(2026, 10, 9, 18, 2, 0, TimeSpan.Zero),
                MerchantName = "Uber Eats",
                Reason = "Order never arrived but the card was charged.",
                Status = DisputeStatus.UnderInvestigation,
                CreatedAt = new DateTimeOffset(2026, 10, 10, 6, 45, 0, TimeSpan.Zero),
                ResolvedAt = (DateTimeOffset?)null
            },
            new
            {
                Id = Guid.Parse("0199c0de-0001-7000-8000-000000000003"),
                TransactionId = Guid.Parse("0199c0de-1001-7000-8000-000000000003"),
                TransactionAmount = 89.50m,
                TransactionCurrency = "ZAR",
                TransactionDate = new DateTimeOffset(2026, 10, 1, 11, 20, 0, TimeSpan.Zero),
                MerchantName = "Spotify",
                Reason = "Subscription billed after cancellation.",
                Status = DisputeStatus.Resolved,
                CreatedAt = new DateTimeOffset(2026, 10, 2, 8, 5, 0, TimeSpan.Zero),
                ResolvedAt = (DateTimeOffset?)new DateTimeOffset(2026, 10, 7, 14, 0, 0, TimeSpan.Zero)
            });
    }
}
