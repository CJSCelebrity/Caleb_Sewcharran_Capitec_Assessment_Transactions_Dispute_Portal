using CapitecDisputes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CapitecDisputes.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(user => user.Id);

        builder.Property(user => user.Email).HasMaxLength(256).IsRequired();
        builder.Property(user => user.PasswordHash).HasMaxLength(512).IsRequired();
        builder.Property(user => user.Role).HasMaxLength(20).IsRequired();

        builder.HasIndex(user => user.Email).IsUnique();
    }
}
