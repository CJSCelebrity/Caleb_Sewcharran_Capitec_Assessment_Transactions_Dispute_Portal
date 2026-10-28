using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CapitecDisputes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CapitecDisputes.Infrastructure.Persistence.Seeding;

internal sealed class TransactionSeeder(
    DisputesDbContext dbContext,
    ILogger<TransactionSeeder> logger)
{
    public async Task SeedAsync(
        IReadOnlyList<SeedTransaction> seedTransactions,
        string source,
        CancellationToken cancellationToken)
    {
        if (seedTransactions.Count == 0)
        {
            logger.LogWarning("{Source} lists no transactions, so none were created.", source);

            return;
        }

        var customerIdByEmail = await dbContext.Users
            .Where(user => user.CustomerId != null)
            .ToDictionaryAsync(user => user.Email, user => user.CustomerId!.Value, cancellationToken);

        var existingReferences = await dbContext.Transactions
            .Select(transaction => transaction.Reference)
            .ToListAsync(cancellationToken);

        var missing = new List<Transaction>();

        foreach (var seedTransaction in seedTransactions)
        {
            if (existingReferences.Contains(seedTransaction.Reference))
            {
                continue;
            }

            if (!customerIdByEmail.TryGetValue(
                    seedTransaction.CustomerEmail.Trim().ToLowerInvariant(),
                    out var customerId))
            {
                logger.LogWarning(
                    "{Source} lists transaction {Reference} for '{Email}', which is not a seeded customer account, so it was skipped.",
                    source,
                    seedTransaction.Reference,
                    seedTransaction.CustomerEmail);

                continue;
            }

            missing.Add(Transaction.Record(
                seedTransaction.Reference,
                customerId,
                seedTransaction.Amount,
                seedTransaction.Currency,
                seedTransaction.OccurredAt,
                seedTransaction.MerchantName));
        }

        if (missing.Count == 0)
        {
            return;
        }

        dbContext.Transactions.AddRange(missing);

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Seeded {Count} transaction(s) from {Source}.", missing.Count, source);
    }
}
