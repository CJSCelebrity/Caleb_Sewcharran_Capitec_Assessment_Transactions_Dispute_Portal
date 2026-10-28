using System;
using System.Linq;
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
    private static readonly Guid CustomerId = Guid.Parse("0199c0de-0003-7000-8000-000000000001");

    private const string Customer = "thandi@example.test";
    private const string Admin = "admin@capitec.test";
    private const string Reason = "Charged twice for a single order.";

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

        dispute.ChangeStatus(DisputeStatus.UnderInvestigation, Admin, ChangedAt);

        dispute.Status.ShouldBe(DisputeStatus.UnderInvestigation);
        dispute.ResolvedAt.ShouldBeNull();
    }

    [Fact]
    public void ChangeStatus_to_resolved_stamps_resolved_at()
    {
        var dispute = RaiseDispute();
        dispute.ChangeStatus(DisputeStatus.UnderInvestigation, Admin, ChangedAt);

        dispute.ChangeStatus(DisputeStatus.Resolved, Admin, ChangedAt);

        dispute.Status.ShouldBe(DisputeStatus.Resolved);
        dispute.ResolvedAt.ShouldBe(ChangedAt);
    }

    [Fact]
    public void ChangeStatus_normalises_resolved_at_to_utc()
    {
        var changedAt = new DateTimeOffset(2026, 10, 21, 9, 30, 0, TimeSpan.FromHours(2));
        var dispute = RaiseDispute();
        dispute.ChangeStatus(DisputeStatus.UnderInvestigation, Admin, changedAt);

        dispute.ChangeStatus(DisputeStatus.Resolved, Admin, changedAt);

        dispute.ResolvedAt!.Value.Offset.ShouldBe(TimeSpan.Zero);
        dispute.ResolvedAt.ShouldBe(changedAt);
        dispute.ResolvedAt!.Value.UtcDateTime.ShouldBe(new DateTime(2026, 10, 21, 7, 30, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void ChangeStatus_rejects_skipping_under_investigation()
    {
        var dispute = RaiseDispute();

        var exception = Should.Throw<InvalidDisputeStatusTransitionException>(
            () => dispute.ChangeStatus(DisputeStatus.Resolved, Admin, ChangedAt));

        exception.From.ShouldBe(DisputeStatus.Initiated);
        exception.To.ShouldBe(DisputeStatus.Resolved);
        dispute.Status.ShouldBe(DisputeStatus.Initiated);
    }

    [Fact]
    public void ChangeStatus_rejects_moving_backwards()
    {
        var dispute = RaiseDispute();
        dispute.ChangeStatus(DisputeStatus.UnderInvestigation, Admin, ChangedAt);

        Should.Throw<InvalidDisputeStatusTransitionException>(
            () => dispute.ChangeStatus(DisputeStatus.Initiated, Admin, ChangedAt));

        dispute.Status.ShouldBe(DisputeStatus.UnderInvestigation);
    }

    [Fact]
    public void ChangeStatus_rejects_repeating_the_current_status()
    {
        var dispute = RaiseDispute();

        Should.Throw<InvalidDisputeStatusTransitionException>(
            () => dispute.ChangeStatus(DisputeStatus.Initiated, Admin, ChangedAt));
    }

    [Fact]
    public void ChangeStatus_rejects_any_move_out_of_resolved()
    {
        var dispute = RaiseDispute();
        dispute.ChangeStatus(DisputeStatus.UnderInvestigation, Admin, ChangedAt);
        dispute.ChangeStatus(DisputeStatus.Resolved, Admin, ChangedAt);

        Should.Throw<InvalidDisputeStatusTransitionException>(
            () => dispute.ChangeStatus(DisputeStatus.UnderInvestigation, Admin, ChangedAt));

        dispute.Status.ShouldBe(DisputeStatus.Resolved);
    }

    [Fact]
    public void ChangeStatus_rejects_a_status_that_is_not_a_defined_enum_value()
    {
        var dispute = RaiseDispute();
        dispute.ChangeStatus(DisputeStatus.UnderInvestigation, Admin, ChangedAt);
        dispute.ChangeStatus(DisputeStatus.Resolved, Admin, ChangedAt);

        var exception = Should.Throw<InvalidDisputeStatusTransitionException>(
            () => dispute.ChangeStatus((DisputeStatus)4, Admin, ChangedAt));

        exception.From.ShouldBe(DisputeStatus.Resolved);
        dispute.Status.ShouldBe(DisputeStatus.Resolved);
    }

    private static Dispute RaiseDispute() => RaiseDisputeAt(TransactionDate, RaisedAt);

    private static Dispute RaiseDisputeAt(DateTimeOffset transactionDate, DateTimeOffset raisedAt) =>
        Dispute.Raise(TransactionOn(transactionDate), Reason, Customer, raisedAt);

    private static Transaction TransactionOn(DateTimeOffset occurredAt) =>
        Transaction.Record("TXN-1001", CustomerId, 1499.99m, "ZAR", occurredAt, "Takealot");

    [Fact]
    public void Raise_copies_the_transaction_detail_and_its_owner()
    {
        var transaction = TransactionOn(TransactionDate);

        var dispute = Dispute.Raise(transaction, Reason, Customer, RaisedAt);

        dispute.CustomerId.ShouldBe(transaction.CustomerId);
        dispute.TransactionId.ShouldBe(transaction.Id);
        dispute.TransactionAmount.ShouldBe(transaction.Amount);
        dispute.TransactionCurrency.ShouldBe(transaction.Currency);
        dispute.TransactionDate.ShouldBe(transaction.OccurredAt);
        dispute.MerchantName.ShouldBe(transaction.MerchantName);
    }

    [Fact]
    public void Raise_records_the_first_history_entry()
    {
        var dispute = RaiseDispute();

        var entry = dispute.StatusChanges.ShouldHaveSingleItem();
        entry.FromStatus.ShouldBeNull();
        entry.ToStatus.ShouldBe(DisputeStatus.Initiated);
        entry.ChangedBy.ShouldBe(Customer);
        entry.ChangedAt.ShouldBe(RaisedAt);
        entry.DisputeId.ShouldBe(dispute.Id);
        entry.Id.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void ChangeStatus_appends_a_history_entry()
    {
        var dispute = RaiseDispute();

        dispute.ChangeStatus(DisputeStatus.UnderInvestigation, Admin, ChangedAt);

        dispute.StatusChanges.Count.ShouldBe(2);
        var latest = dispute.StatusChanges.Last();
        latest.FromStatus.ShouldBe(DisputeStatus.Initiated);
        latest.ToStatus.ShouldBe(DisputeStatus.UnderInvestigation);
        latest.ChangedBy.ShouldBe(Admin);
        latest.ChangedAt.ShouldBe(ChangedAt);
    }

    [Fact]
    public void ChangeStatus_normalises_the_history_timestamp_to_utc()
    {
        var dispute = RaiseDispute();

        dispute.ChangeStatus(
            DisputeStatus.UnderInvestigation,
            Admin,
            new DateTimeOffset(2026, 10, 21, 11, 30, 0, TimeSpan.FromHours(2)));

        dispute.StatusChanges.Last().ChangedAt.Offset.ShouldBe(TimeSpan.Zero);
        dispute.StatusChanges.Last().ChangedAt.UtcDateTime
            .ShouldBe(new DateTime(2026, 10, 21, 9, 30, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void ChangeStatus_records_the_whole_journey_to_resolved()
    {
        var dispute = RaiseDispute();

        dispute.ChangeStatus(DisputeStatus.UnderInvestigation, Admin, ChangedAt);
        dispute.ChangeStatus(DisputeStatus.Resolved, Admin, ChangedAt);

        dispute.StatusChanges.Count.ShouldBe(3);
        dispute.StatusChanges.Select(change => change.ToStatus).ShouldBe(
        [
            DisputeStatus.Initiated,
            DisputeStatus.UnderInvestigation,
            DisputeStatus.Resolved
        ]);
    }

    [Fact]
    public void ChangeStatus_records_nothing_when_the_transition_is_rejected()
    {
        var dispute = RaiseDispute();

        Should.Throw<InvalidDisputeStatusTransitionException>(
            () => dispute.ChangeStatus(DisputeStatus.Resolved, Admin, ChangedAt));

        dispute.StatusChanges.ShouldHaveSingleItem();
    }
}
