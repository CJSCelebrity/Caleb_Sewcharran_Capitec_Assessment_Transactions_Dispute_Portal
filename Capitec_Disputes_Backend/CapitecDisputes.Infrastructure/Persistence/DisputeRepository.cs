using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CapitecDisputes.Application.Abstractions;
using CapitecDisputes.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CapitecDisputes.Infrastructure.Persistence;

internal sealed class DisputeRepository(DisputesDbContext dbContext) : IDisputeRepository
{
    public async Task<IReadOnlyList<Dispute>> ListAsync(
        DisputeStatus? status,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Disputes.AsNoTracking();

        if (status is not null)
        {
            var filter = status.Value;
            query = query.Where(dispute => dispute.Status == filter);
        }

        return await query
            .OrderByDescending(dispute => dispute.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<Dispute?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Disputes.FirstOrDefaultAsync(dispute => dispute.Id == id, cancellationToken);

    public void Add(Dispute dispute) => dbContext.Disputes.Add(dispute);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
