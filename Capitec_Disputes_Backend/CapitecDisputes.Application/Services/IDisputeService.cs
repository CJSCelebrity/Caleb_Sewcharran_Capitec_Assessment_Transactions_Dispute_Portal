using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CapitecDisputes.Application.Authorization;
using CapitecDisputes.Application.Contracts;
using CapitecDisputes.Domain.Entities;

namespace CapitecDisputes.Application.Services;

public interface IDisputeService
{
    Task<IReadOnlyList<DisputeResponse>> GetAllAsync(
        CallerContext caller,
        DisputeStatus? status,
        CancellationToken cancellationToken);

    Task<DisputeResponse?> GetByIdAsync(CallerContext caller, Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<DisputeStatusChangeResponse>?> GetHistoryAsync(
        CallerContext caller,
        Guid id,
        CancellationToken cancellationToken);

    Task<DisputeResponse?> CreateAsync(
        CallerContext caller,
        CreateDisputeRequest request,
        CancellationToken cancellationToken);

    Task<DisputeResponse?> ChangeStatusAsync(
        CallerContext caller,
        Guid id,
        DisputeStatus status,
        CancellationToken cancellationToken);
}
