using CapitecDisputes.Domain.Exceptions;

namespace CapitecDisputes.Domain.Entities;

public sealed class Dispute
{
    private readonly List<DisputeStatusChange> _statusChanges = [];

    private Dispute()
    {
    }

    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public Guid TransactionId { get; private set; }
    public decimal TransactionAmount { get; private set; }
    public string TransactionCurrency { get; private set; } = string.Empty;
    public DateTimeOffset TransactionDate { get; private set; }
    public string MerchantName { get; private set; } = string.Empty;
    public string Reason { get; private set; } = string.Empty;
    public DisputeStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ResolvedAt { get; private set; }

    public IReadOnlyCollection<DisputeStatusChange> StatusChanges => _statusChanges;

    public static Dispute Raise(
        Transaction transaction,
        string reason,
        string raisedBy,
        DateTimeOffset raisedAt)
    {
        var dispute = new Dispute
        {
            Id = Guid.CreateVersion7(),
            CustomerId = transaction.CustomerId,
            TransactionId = transaction.Id,
            TransactionAmount = transaction.Amount,
            TransactionCurrency = transaction.Currency,
            TransactionDate = transaction.OccurredAt,
            MerchantName = transaction.MerchantName,
            Reason = reason,
            Status = DisputeStatus.Initiated,
            CreatedAt = raisedAt.ToUniversalTime()
        };

        dispute._statusChanges.Add(DisputeStatusChange.Record(
            dispute.Id,
            null,
            DisputeStatus.Initiated,
            raisedBy,
            dispute.CreatedAt));

        return dispute;
    }

    public void ChangeStatus(DisputeStatus next, string changedBy, DateTimeOffset changedAt)
    {
        if (!Enum.IsDefined(next))
        {
            throw new InvalidDisputeStatusTransitionException(Status, next);
        }

        if ((int)next != (int)Status + 1)
        {
            throw new InvalidDisputeStatusTransitionException(Status, next);
        }

        var from = Status;

        Status = next;

        if (next == DisputeStatus.Resolved)
        {
            ResolvedAt = changedAt.ToUniversalTime();
        }

        _statusChanges.Add(DisputeStatusChange.Record(Id, from, next, changedBy, changedAt));
    }
}
