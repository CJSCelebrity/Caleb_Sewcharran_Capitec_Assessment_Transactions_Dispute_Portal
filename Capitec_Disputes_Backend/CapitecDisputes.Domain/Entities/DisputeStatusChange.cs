namespace CapitecDisputes.Domain.Entities;

public sealed class DisputeStatusChange
{
    private DisputeStatusChange()
    {
    }

    public Guid Id { get; private set; }
    public Guid DisputeId { get; private set; }
    public DisputeStatus? FromStatus { get; private set; }
    public DisputeStatus ToStatus { get; private set; }
    public string ChangedBy { get; private set; } = string.Empty;
    public DateTimeOffset ChangedAt { get; private set; }

    internal static DisputeStatusChange Record(
        Guid disputeId,
        DisputeStatus? fromStatus,
        DisputeStatus toStatus,
        string changedBy,
        DateTimeOffset changedAt) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            DisputeId = disputeId,
            FromStatus = fromStatus,
            ToStatus = toStatus,
            ChangedBy = changedBy.Trim(),
            ChangedAt = changedAt.ToUniversalTime()
        };
}
