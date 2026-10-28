using System;
using CapitecDisputes.Domain.Entities;
using Shouldly;

namespace CapitecDisputes.Tests.Entities;

public class TransactionTests
{
    private static Transaction Record(
        string reference = "TXN-1001",
        string currency = "ZAR",
        string merchantName = "Takealot",
        DateTimeOffset? occurredAt = null) =>
        Transaction.Record(
            reference,
            Guid.CreateVersion7(),
            1499.99m,
            currency,
            occurredAt ?? new DateTimeOffset(2026, 10, 2, 9, 14, 0, TimeSpan.Zero),
            merchantName);

    [Fact]
    public void Record_normalises_the_timestamp_to_utc()
    {
        var occurredAt = new DateTimeOffset(2026, 3, 1, 12, 0, 0, TimeSpan.FromHours(2));

        var transaction = Record(occurredAt: occurredAt);

        transaction.OccurredAt.Offset.ShouldBe(TimeSpan.Zero);
        transaction.OccurredAt.ShouldBe(occurredAt.ToUniversalTime());
        transaction.OccurredAt.Hour.ShouldBe(10);
    }

    [Fact]
    public void Record_uppercases_and_trims_the_currency()
    {
        Record(currency: " zar ").Currency.ShouldBe("ZAR");
    }

    [Fact]
    public void Record_trims_the_reference_and_the_merchant_name()
    {
        var transaction = Record(reference: "  TXN-1001  ", merchantName: "  Takealot  ");

        transaction.Reference.ShouldBe("TXN-1001");
        transaction.MerchantName.ShouldBe("Takealot");
    }

    [Fact]
    public void Record_keeps_the_owner_and_the_amount()
    {
        var customerId = Guid.CreateVersion7();

        var transaction = Transaction.Record(
            "TXN-1001",
            customerId,
            320.00m,
            "ZAR",
            new DateTimeOffset(2026, 10, 5, 18, 42, 0, TimeSpan.Zero),
            "Uber Eats");

        transaction.CustomerId.ShouldBe(customerId);
        transaction.Amount.ShouldBe(320.00m);
    }

    [Fact]
    public void Record_assigns_an_id()
    {
        Record().Id.ShouldNotBe(Guid.Empty);
    }
}
