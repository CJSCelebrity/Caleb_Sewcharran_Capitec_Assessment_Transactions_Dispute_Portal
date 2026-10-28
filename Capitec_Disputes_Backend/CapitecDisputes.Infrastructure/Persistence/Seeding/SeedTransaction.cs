using System;

namespace CapitecDisputes.Infrastructure.Persistence.Seeding;

internal sealed record SeedTransaction(
    string Reference,
    string CustomerEmail,
    decimal Amount,
    string Currency,
    DateTimeOffset OccurredAt,
    string MerchantName);
