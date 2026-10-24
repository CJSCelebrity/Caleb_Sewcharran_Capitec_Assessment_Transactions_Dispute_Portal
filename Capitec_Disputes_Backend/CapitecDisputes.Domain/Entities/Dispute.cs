using CapitecDisputes.Domain.Exceptions;

namespace CapitecDisputes.Domain.Entities;

public sealed class Dispute
{
    private Dispute()
    {
    }

    public Guid Id { get; private set; }
    public Guid TransactionId { get; private set; }
    public decimal TransactionAmount { get; private set; }
    public string TransactionCurrency { get; private set; } = string.Empty;
    public DateTimeOffset TransactionDate { get; private set; }
    public string MerchantName { get; private set; } = string.Empty;
    public string Reason { get; private set; } = string.Empty;
    public DisputeStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ResolvedAt { get; private set; }

    public static Dispute Raise(
        Guid transactionId,
        decimal transactionAmount,
        string transactionCurrency,
        DateTimeOffset transactionDate,
        string merchantName,
        string reason,
        DateTimeOffset raisedAt) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            TransactionId = transactionId,
            TransactionAmount = transactionAmount,
            TransactionCurrency = transactionCurrency,
            TransactionDate = transactionDate.ToUniversalTime(),
            MerchantName = merchantName,
            Reason = reason,
            Status = DisputeStatus.Initiated,
            CreatedAt = raisedAt.ToUniversalTime()
        };

    public void ChangeStatus(DisputeStatus next, DateTimeOffset changedAt)
    {
        if (!Enum.IsDefined(next))
        {
            throw new InvalidDisputeStatusTransitionException(Status, next);
        }

        if ((int)next != (int)Status + 1)
        {
            throw new InvalidDisputeStatusTransitionException(Status, next);
        }

        Status = next;

        if (next == DisputeStatus.Resolved)
        {
            ResolvedAt = changedAt.ToUniversalTime();
        }
    }
}
