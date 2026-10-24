using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CapitecDisputes.Domain.Entities;

namespace CapitecDisputes.Application.Abstractions;

public interface IDisputeRepository
{
    Task<IReadOnlyList<Dispute>> ListAsync(DisputeStatus? status, CancellationToken cancellationToken);

    Task<Dispute?> FindAsync(Guid id, CancellationToken cancellationToken);

    void Add(Dispute dispute);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
