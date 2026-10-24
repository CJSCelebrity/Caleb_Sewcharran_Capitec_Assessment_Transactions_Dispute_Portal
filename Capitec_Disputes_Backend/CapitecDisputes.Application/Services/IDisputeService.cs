using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CapitecDisputes.Application.Contracts;
using CapitecDisputes.Domain.Entities;

namespace CapitecDisputes.Application.Services;

public interface IDisputeService
{
    Task<IReadOnlyList<DisputeResponse>> GetAllAsync(DisputeStatus? status, CancellationToken cancellationToken);

    Task<DisputeResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<DisputeResponse> CreateAsync(CreateDisputeRequest request, CancellationToken cancellationToken);

    Task<DisputeResponse?> ChangeStatusAsync(Guid id, DisputeStatus status, CancellationToken cancellationToken);
}
