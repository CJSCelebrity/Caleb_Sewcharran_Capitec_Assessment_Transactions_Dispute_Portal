using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CapitecDisputes.Application.Abstractions;
using CapitecDisputes.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CapitecDisputes.Infrastructure.Persistence;

internal sealed class TransactionRepository(DisputesDbContext dbContext) : ITransactionRepository
{
    public async Task<IReadOnlyList<Transaction>> ListForCustomerAsync(
        Guid customerId,
        CancellationToken cancellationToken) =>
        await dbContext.Transactions
            .AsNoTracking()
            .Where(transaction => transaction.CustomerId == customerId)
            .OrderByDescending(transaction => transaction.OccurredAt)
            .ToListAsync(cancellationToken);

    public Task<Transaction?> FindForCustomerAsync(
        Guid id,
        Guid customerId,
        CancellationToken cancellationToken) =>
        dbContext.Transactions
            .AsNoTracking()
            .FirstOrDefaultAsync(
                transaction => transaction.Id == id && transaction.CustomerId == customerId,
                cancellationToken);
}
