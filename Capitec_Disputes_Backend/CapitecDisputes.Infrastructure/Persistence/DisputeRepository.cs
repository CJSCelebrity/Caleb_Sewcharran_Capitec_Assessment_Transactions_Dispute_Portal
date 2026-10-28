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
        Guid? customerId,
        DisputeStatus? status,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Disputes.AsNoTracking();

        if (customerId is { } owner)
        {
            query = query.Where(dispute => dispute.CustomerId == owner);
        }

        if (status is { } filter)
        {
            query = query.Where(dispute => dispute.Status == filter);
        }

        return await query
            .OrderByDescending(dispute => dispute.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<Dispute?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Disputes.FirstOrDefaultAsync(dispute => dispute.Id == id, cancellationToken);

    public Task<Dispute?> FindWithHistoryAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Disputes
            .Include(dispute => dispute.StatusChanges)
            .FirstOrDefaultAsync(dispute => dispute.Id == id, cancellationToken);

    public Task<Dispute?> FindForTransactionAsync(Guid transactionId, CancellationToken cancellationToken) =>
        dbContext.Disputes
            .AsNoTracking()
            .FirstOrDefaultAsync(dispute => dispute.TransactionId == transactionId, cancellationToken);

    public void Add(Dispute dispute) => dbContext.Disputes.Add(dispute);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
