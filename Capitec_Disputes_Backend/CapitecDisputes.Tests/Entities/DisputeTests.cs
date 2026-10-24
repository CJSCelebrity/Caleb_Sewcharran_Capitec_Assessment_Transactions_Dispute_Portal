using System;
using CapitecDisputes.Domain.Entities;
using CapitecDisputes.Domain.Exceptions;
using Shouldly;
using Xunit;

namespace CapitecDisputes.Tests.Entities;

public class DisputeTests
{
    private static readonly DateTimeOffset TransactionDate = new(2026, 10, 12, 9, 14, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset RaisedAt = new(2026, 10, 20, 8, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset ChangedAt = new(2026, 10, 21, 9, 30, 0, TimeSpan.Zero);

    [Fact]
    public void Raise_starts_the_dispute_as_initiated()
    {
        var dispute = RaiseDispute();

        dispute.Status.ShouldBe(DisputeStatus.Initiated);
        dispute.CreatedAt.ShouldBe(RaisedAt);
        dispute.ResolvedAt.ShouldBeNull();
        dispute.Id.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void Raise_keeps_the_transaction_details_it_was_given()
    {
        var dispute = RaiseDispute();

        dispute.TransactionAmount.ShouldBe(1499.99m);
        dispute.TransactionCurrency.ShouldBe("ZAR");
        dispute.MerchantName.ShouldBe("Takealot");
        dispute.Reason.ShouldBe("Charged twice for a single order.");
    }

    [Fact]
    public void Raise_normalises_the_transaction_date_to_utc()
    {
        var transactionDate = new DateTimeOffset(2026, 10, 20, 12, 5, 0, TimeSpan.FromHours(2));

        var dispute = RaiseDisputeAt(transactionDate, RaisedAt);

        dispute.TransactionDate.Offset.ShouldBe(TimeSpan.Zero);
        dispute.TransactionDate.ShouldBe(transactionDate);
        dispute.TransactionDate.UtcDateTime.ShouldBe(new DateTime(2026, 10, 20, 10, 5, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void Raise_normalises_the_raised_at_timestamp_to_utc()
    {
        var raisedAt = new DateTimeOffset(2026, 10, 20, 8, 0, 0, TimeSpan.FromHours(2));

        var dispute = RaiseDisputeAt(TransactionDate, raisedAt);

        dispute.CreatedAt.Offset.ShouldBe(TimeSpan.Zero);
        dispute.CreatedAt.ShouldBe(raisedAt);
        dispute.CreatedAt.UtcDateTime.ShouldBe(new DateTime(2026, 10, 20, 6, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void ChangeStatus_moves_initiated_to_under_investigation()
    {
        var dispute = RaiseDispute();

        dispute.ChangeStatus(DisputeStatus.UnderInvestigation, ChangedAt);

        dispute.Status.ShouldBe(DisputeStatus.UnderInvestigation);
        dispute.ResolvedAt.ShouldBeNull();
    }

    [Fact]
    public void ChangeStatus_to_resolved_stamps_resolved_at()
    {
        var dispute = RaiseDispute();
        dispute.ChangeStatus(DisputeStatus.UnderInvestigation, ChangedAt);

        dispute.ChangeStatus(DisputeStatus.Resolved, ChangedAt);

        dispute.Status.ShouldBe(DisputeStatus.Resolved);
        dispute.ResolvedAt.ShouldBe(ChangedAt);
    }

    [Fact]
    public void ChangeStatus_normalises_resolved_at_to_utc()
    {
        var changedAt = new DateTimeOffset(2026, 10, 21, 9, 30, 0, TimeSpan.FromHours(2));
        var dispute = RaiseDispute();
        dispute.ChangeStatus(DisputeStatus.UnderInvestigation, changedAt);

        dispute.ChangeStatus(DisputeStatus.Resolved, changedAt);

        dispute.ResolvedAt!.Value.Offset.ShouldBe(TimeSpan.Zero);
        dispute.ResolvedAt.ShouldBe(changedAt);
        dispute.ResolvedAt!.Value.UtcDateTime.ShouldBe(new DateTime(2026, 10, 21, 7, 30, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void ChangeStatus_rejects_skipping_under_investigation()
    {
        var dispute = RaiseDispute();

        var exception = Should.Throw<InvalidDisputeStatusTransitionException>(
            () => dispute.ChangeStatus(DisputeStatus.Resolved, ChangedAt));

        exception.From.ShouldBe(DisputeStatus.Initiated);
        exception.To.ShouldBe(DisputeStatus.Resolved);
        dispute.Status.ShouldBe(DisputeStatus.Initiated);
    }

    [Fact]
    public void ChangeStatus_rejects_moving_backwards()
    {
        var dispute = RaiseDispute();
        dispute.ChangeStatus(DisputeStatus.UnderInvestigation, ChangedAt);

        Should.Throw<InvalidDisputeStatusTransitionException>(
            () => dispute.ChangeStatus(DisputeStatus.Initiated, ChangedAt));

        dispute.Status.ShouldBe(DisputeStatus.UnderInvestigation);
    }

    [Fact]
    public void ChangeStatus_rejects_repeating_the_current_status()
    {
        var dispute = RaiseDispute();

        Should.Throw<InvalidDisputeStatusTransitionException>(
            () => dispute.ChangeStatus(DisputeStatus.Initiated, ChangedAt));
    }

    [Fact]
    public void ChangeStatus_rejects_any_move_out_of_resolved()
    {
        var dispute = RaiseDispute();
        dispute.ChangeStatus(DisputeStatus.UnderInvestigation, ChangedAt);
        dispute.ChangeStatus(DisputeStatus.Resolved, ChangedAt);

        Should.Throw<InvalidDisputeStatusTransitionException>(
            () => dispute.ChangeStatus(DisputeStatus.UnderInvestigation, ChangedAt));

        dispute.Status.ShouldBe(DisputeStatus.Resolved);
    }

    [Fact]
    public void ChangeStatus_rejects_a_status_that_is_not_a_defined_enum_value()
    {
        var dispute = RaiseDispute();
        dispute.ChangeStatus(DisputeStatus.UnderInvestigation, ChangedAt);
        dispute.ChangeStatus(DisputeStatus.Resolved, ChangedAt);

        var exception = Should.Throw<InvalidDisputeStatusTransitionException>(
            () => dispute.ChangeStatus((DisputeStatus)4, ChangedAt));

        exception.From.ShouldBe(DisputeStatus.Resolved);
        dispute.Status.ShouldBe(DisputeStatus.Resolved);
    }

    private static Dispute RaiseDispute() => RaiseDisputeAt(TransactionDate, RaisedAt);

    private static Dispute RaiseDisputeAt(DateTimeOffset transactionDate, DateTimeOffset raisedAt) =>
        Dispute.Raise(
            Guid.Parse("0199c0de-1001-7000-8000-000000000001"),
            1499.99m,
            "ZAR",
            transactionDate,
            "Takealot",
            "Charged twice for a single order.",
            raisedAt);
}
