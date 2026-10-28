using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CapitecDisputes.Domain.Entities;

namespace CapitecDisputes.Application.Abstractions;

public interface ITransactionRepository
{
    Task<IReadOnlyList<Transaction>> ListForCustomerAsync(Guid customerId, CancellationToken cancellationToken);

    Task<Transaction?> FindForCustomerAsync(Guid id, Guid customerId, CancellationToken cancellationToken);
}
