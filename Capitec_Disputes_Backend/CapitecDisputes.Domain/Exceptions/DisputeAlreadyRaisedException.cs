namespace CapitecDisputes.Domain.Exceptions;

public sealed class DisputeAlreadyRaisedException(Guid transactionId, Guid existingDisputeId)
    : InvalidOperationException($"Transaction {transactionId} already has dispute {existingDisputeId}.")
{
    public Guid TransactionId { get; } = transactionId;

    public Guid ExistingDisputeId { get; } = existingDisputeId;
}
