using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CapitecDisputes.Application.Abstractions;
using CapitecDisputes.Application.Authorization;
using CapitecDisputes.Application.Contracts;
using CapitecDisputes.Domain.Entities;
using CapitecDisputes.Domain.Exceptions;

namespace CapitecDisputes.Application.Services;

public sealed class DisputeService(
    IDisputeRepository repository,
    ITransactionRepository transactionRepository,
    TimeProvider timeProvider) : IDisputeService
{
    public async Task<IReadOnlyList<DisputeResponse>> GetAllAsync(
        CallerContext caller,
        DisputeStatus? status,
        CancellationToken cancellationToken)
    {
        if (IsCustomerWithoutAnIdentity(caller))
        {
            return [];
        }

        var disputes = await repository.ListAsync(CustomerFilterFor(caller), status, cancellationToken);

        return disputes.Select(ToResponse).ToList();
    }

    public async Task<DisputeResponse?> GetByIdAsync(
        CallerContext caller,
        Guid id,
        CancellationToken cancellationToken)
    {
        var dispute = await repository.FindAsync(id, cancellationToken);

        return dispute is null || !CanSee(caller, dispute) ? null : ToResponse(dispute);
    }

    public async Task<IReadOnlyList<DisputeStatusChangeResponse>?> GetHistoryAsync(
        CallerContext caller,
        Guid id,
        CancellationToken cancellationToken)
    {
        var dispute = await repository.FindWithHistoryAsync(id, cancellationToken);

        if (dispute is null || !CanSee(caller, dispute))
        {
            return null;
        }

        return dispute.StatusChanges
            .OrderBy(change => change.ChangedAt)
            .Select(change => new DisputeStatusChangeResponse(
                change.FromStatus?.ToString(),
                change.ToStatus.ToString(),
                change.ChangedBy,
                change.ChangedAt))
            .ToList();
    }

    public async Task<DisputeResponse?> CreateAsync(
        CallerContext caller,
        CreateDisputeRequest request,
        CancellationToken cancellationToken)
    {
        if (caller.CustomerId is not { } customerId)
        {
            return null;
        }

        var transaction = await transactionRepository.FindForCustomerAsync(
            request.TransactionId!.Value,
            customerId,
            cancellationToken);

        if (transaction is null)
        {
            return null;
        }

        var existing = await repository.FindForTransactionAsync(transaction.Id, cancellationToken);

        if (existing is not null)
        {
            throw new DisputeAlreadyRaisedException(transaction.Id, existing.Id);
        }

        var dispute = Dispute.Raise(transaction, request.Reason, caller.Email, timeProvider.GetUtcNow());

        repository.Add(dispute);
        await repository.SaveChangesAsync(cancellationToken);

        return ToResponse(dispute);
    }

    public async Task<DisputeResponse?> ChangeStatusAsync(
        CallerContext caller,
        Guid id,
        DisputeStatus status,
        CancellationToken cancellationToken)
    {
        var dispute = await repository.FindWithHistoryAsync(id, cancellationToken);

        if (dispute is null || !CanSee(caller, dispute))
        {
            return null;
        }

        dispute.ChangeStatus(status, caller.Email, timeProvider.GetUtcNow());
        await repository.SaveChangesAsync(cancellationToken);

        return ToResponse(dispute);
    }

    private static bool CanSee(CallerContext caller, Dispute dispute) =>
        !caller.IsCustomer || (caller.CustomerId is { } customerId && dispute.CustomerId == customerId);

    private static Guid? CustomerFilterFor(CallerContext caller) =>
        caller.IsCustomer ? caller.CustomerId : null;

    private static bool IsCustomerWithoutAnIdentity(CallerContext caller) =>
        caller.IsCustomer && caller.CustomerId is null;

    private static DisputeResponse ToResponse(Dispute dispute) =>
        new(
            dispute.Id,
            dispute.TransactionId,
            dispute.TransactionAmount,
            dispute.TransactionCurrency,
            dispute.TransactionDate,
            dispute.MerchantName,
            dispute.Reason,
            dispute.Status.ToString(),
            dispute.CreatedAt,
            dispute.ResolvedAt);
}
