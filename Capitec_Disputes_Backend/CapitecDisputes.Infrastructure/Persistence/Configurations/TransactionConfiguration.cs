using CapitecDisputes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CapitecDisputes.Infrastructure.Persistence.Configurations;

internal sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("transactions");
        builder.HasKey(transaction => transaction.Id);
        builder.Property(transaction => transaction.Id).ValueGeneratedNever();

        builder.Property(transaction => transaction.Reference).HasMaxLength(32).IsRequired();
        builder.Property(transaction => transaction.Amount).HasPrecision(18, 2);
        builder.Property(transaction => transaction.Currency).HasMaxLength(3).IsRequired();
        builder.Property(transaction => transaction.MerchantName).HasMaxLength(200).IsRequired();

        builder.HasIndex(transaction => transaction.Reference).IsUnique();
        builder.HasIndex(transaction => transaction.CustomerId);
    }
}
