using CapitecDisputes.Domain.Entities;

namespace CapitecDisputes.Infrastructure.Persistence.Seeding;

internal sealed record SeedDispute(string TransactionReference, DisputeStatus Status, string Reason);
