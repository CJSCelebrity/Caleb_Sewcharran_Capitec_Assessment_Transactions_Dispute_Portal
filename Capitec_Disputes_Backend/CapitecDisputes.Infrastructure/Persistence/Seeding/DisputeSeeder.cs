using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CapitecDisputes.Application.Authorization;
using CapitecDisputes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CapitecDisputes.Infrastructure.Persistence.Seeding;

internal sealed class DisputeSeeder(
    DisputesDbContext dbContext,
    TimeProvider timeProvider,
    ILogger<DisputeSeeder> logger)
{
    private const string StaffActor = "admin@capitec.test";

    public async Task SeedAsync(
        IReadOnlyList<SeedDispute> seedDisputes,
        string source,
        CancellationToken cancellationToken)
    {
        if (seedDisputes.Count == 0)
        {
            logger.LogWarning("{Source} lists no disputes, so none were created.", source);

            return;
        }

        var references = seedDisputes.Select(dispute => dispute.TransactionReference).ToList();

        var transactions = await dbContext.Transactions
            .Where(transaction => references.Contains(transaction.Reference))
            .ToDictionaryAsync(transaction => transaction.Reference, cancellationToken);

        var alreadyDisputed = await dbContext.Disputes
            .Select(dispute => dispute.TransactionId)
            .ToListAsync(cancellationToken);

        var emailByCustomerId = await dbContext.Users
            .Where(user => user.CustomerId != null)
            .ToDictionaryAsync(user => user.CustomerId!.Value, user => user.Email, cancellationToken);

        var raised = new List<Dispute>();

        foreach (var seedDispute in seedDisputes)
        {
            if (!transactions.TryGetValue(seedDispute.TransactionReference, out var transaction))
            {
                logger.LogWarning(
                    "{Source} disputes transaction {Reference}, which does not exist, so it was skipped.",
                    source,
                    seedDispute.TransactionReference);

                continue;
            }

            if (alreadyDisputed.Contains(transaction.Id))
            {
                continue;
            }

            var raisedBy = emailByCustomerId.GetValueOrDefault(transaction.CustomerId, Roles.Customer);
            var dispute = Dispute.Raise(
                transaction,
                seedDispute.Reason,
                raisedBy,
                timeProvider.GetUtcNow());

            AdvanceTo(dispute, seedDispute.Status);

            raised.Add(dispute);
        }

        if (raised.Count == 0)
        {
            return;
        }

        dbContext.Disputes.AddRange(raised);

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Seeded {Count} dispute(s) from {Source}.", raised.Count, source);
    }

    private void AdvanceTo(Dispute dispute, DisputeStatus target)
    {
        while (dispute.Status != target)
        {
            dispute.ChangeStatus(dispute.Status + 1, StaffActor, timeProvider.GetUtcNow());
        }
    }
}
