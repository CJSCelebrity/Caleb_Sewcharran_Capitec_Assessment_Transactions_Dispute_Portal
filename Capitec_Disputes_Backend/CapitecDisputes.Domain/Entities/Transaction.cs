namespace CapitecDisputes.Domain.Entities;

public sealed class Transaction
{
    private Transaction()
    {
    }

    public Guid Id { get; private set; }
    public string Reference { get; private set; } = string.Empty;
    public Guid CustomerId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; private set; }
    public string MerchantName { get; private set; } = string.Empty;

    public static Transaction Record(
        string reference,
        Guid customerId,
        decimal amount,
        string currency,
        DateTimeOffset occurredAt,
        string merchantName) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            Reference = reference.Trim(),
            CustomerId = customerId,
            Amount = amount,
            Currency = currency.Trim().ToUpperInvariant(),
            OccurredAt = occurredAt.ToUniversalTime(),
            MerchantName = merchantName.Trim()
        };
}
