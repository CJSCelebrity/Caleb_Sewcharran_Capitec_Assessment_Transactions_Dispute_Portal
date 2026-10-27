using CapitecDisputes.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CapitecDisputes.Infrastructure.Persistence;

public sealed class DisputesDbContext(DbContextOptions<DisputesDbContext> options) : DbContext(options)
{
    public DbSet<Dispute> Disputes => Set<Dispute>();

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DisputesDbContext).Assembly);
}
