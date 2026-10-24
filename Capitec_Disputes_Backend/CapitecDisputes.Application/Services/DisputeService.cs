using CapitecDisputes.Application.Abstractions;
using CapitecDisputes.Application.Contracts;
using CapitecDisputes.Domain.Entities;

namespace CapitecDisputes.Application.Services;

public sealed class DisputeService(IDisputeRepository repository, TimeProvider timeProvider) : IDisputeService
{
    public async Task<IReadOnlyList<DisputeResponse>> GetAllAsync(
        DisputeStatus? status,
        CancellationToken cancellationToken)
    {
        var disputes = await repository.ListAsync(status, cancellationToken);

        return disputes.Select(ToResponse).ToList();
    }

    public async Task<DisputeResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var dispute = await repository.FindAsync(id, cancellationToken);

        return dispute is null ? null : ToResponse(dispute);
    }

    public async Task<DisputeResponse> CreateAsync(
        CreateDisputeRequest request,
        CancellationToken cancellationToken)
    {
        var dispute = Dispute.Raise(
            request.TransactionId!.Value,
            request.TransactionAmount!.Value,
            request.TransactionCurrency.ToUpperInvariant(),
            request.TransactionDate!.Value,
            request.MerchantName,
            request.Reason,
            timeProvider.GetUtcNow());

        repository.Add(dispute);
        await repository.SaveChangesAsync(cancellationToken);

        return ToResponse(dispute);
    }

    public async Task<DisputeResponse?> ChangeStatusAsync(
        Guid id,
        DisputeStatus status,
        CancellationToken cancellationToken)
    {
        var dispute = await repository.FindAsync(id, cancellationToken);

        if (dispute is null)
        {
            return null;
        }

        dispute.ChangeStatus(status, timeProvider.GetUtcNow());
        await repository.SaveChangesAsync(cancellationToken);

        return ToResponse(dispute);
    }

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
