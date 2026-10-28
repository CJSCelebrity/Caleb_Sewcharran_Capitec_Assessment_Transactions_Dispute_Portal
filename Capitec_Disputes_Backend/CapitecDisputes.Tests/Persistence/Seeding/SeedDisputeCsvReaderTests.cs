using CapitecDisputes.Domain.Entities;
using CapitecDisputes.Infrastructure.Persistence.Seeding;
using Shouldly;

namespace CapitecDisputes.Tests.Persistence.Seeding;

public class SeedDisputeCsvReaderTests
{
    private const string Header = "transactionReference,status,reason";

    private static IReadOnlyList<SeedDispute> Read(string csv) =>
        SeedDisputeCsvReader.Read(new StringReader(csv), "seed-disputes.csv");

    [Fact]
    public void Read_returns_a_dispute_for_each_data_row()
    {
        var disputes = Read(
            $"""
            {Header}
            TXN-1001,Resolved,Charged twice for a single order.
            TXN-1002,UnderInvestigation,Order never arrived.
            """);

        disputes.Count.ShouldBe(2);
        disputes[0].ShouldBe(new SeedDispute(
            "TXN-1001",
            DisputeStatus.Resolved,
            "Charged twice for a single order."));
        disputes[1].Status.ShouldBe(DisputeStatus.UnderInvestigation);
    }

    [Fact]
    public void Read_trims_surrounding_whitespace_on_every_field()
    {
        var disputes = Read(
            $"""
            {Header}
              TXN-1001 , Resolved , Charged twice.
            """);

        disputes[0].ShouldBe(new SeedDispute("TXN-1001", DisputeStatus.Resolved, "Charged twice."));
    }

    [Fact]
    public void Read_keeps_a_reason_that_contains_commas()
    {
        var disputes = Read(
            $"""
            {Header}
            TXN-1001,Initiated,Charged twice, then refunded, then charged again.
            """);

        disputes[0].Reason.ShouldBe("Charged twice, then refunded, then charged again.");
    }

    [Fact]
    public void Read_skips_blank_lines()
    {
        var disputes = Read(
            $"""
            {Header}

            TXN-1001,Initiated,Charged twice.

            """);

        disputes.ShouldHaveSingleItem();
    }

    [Fact]
    public void Read_returns_nothing_for_a_header_only_file()
    {
        Read(Header).ShouldBeEmpty();
    }

    [Fact]
    public void Read_throws_when_the_header_is_wrong()
    {
        Should.Throw<InvalidOperationException>(() => Read(
            """
            reference,reason
            TXN-1001,Charged twice.
            """))
            .Message.ShouldContain("seed-disputes.csv line 1");
    }

    [Fact]
    public void Read_throws_for_an_empty_file()
    {
        Should.Throw<InvalidOperationException>(() => Read(string.Empty))
            .Message.ShouldContain("seed-disputes.csv");
    }

    [Fact]
    public void Read_throws_when_a_row_has_too_few_fields()
    {
        Should.Throw<InvalidOperationException>(() => Read(
            $"""
            {Header}
            TXN-1001,Initiated
            """))
            .Message.ShouldContain("line 2");
    }

    [Fact]
    public void Read_throws_when_the_status_is_not_a_dispute_status()
    {
        Should.Throw<InvalidOperationException>(() => Read(
            $"""
            {Header}
            TXN-1001,Pending,Charged twice.
            """))
            .Message.ShouldContain("line 2");
    }

    [Fact]
    public void Read_throws_when_the_reason_is_blank()
    {
        Should.Throw<InvalidOperationException>(() => Read(
            $"""
            {Header}
            TXN-1001,Initiated,
            """))
            .Message.ShouldContain("line 2");
    }

    [Fact]
    public void Read_throws_when_the_transaction_reference_is_blank()
    {
        Should.Throw<InvalidOperationException>(() => Read(
            $"""
            {Header}
            ,Initiated,Charged twice.
            """))
            .Message.ShouldContain("line 2");
    }

    [Fact]
    public void Read_throws_when_a_transaction_is_disputed_twice()
    {
        Should.Throw<InvalidOperationException>(() => Read(
            $"""
            {Header}
            TXN-1001,Initiated,Charged twice.
            TXN-1001,Resolved,Charged twice again.
            """))
            .Message.ShouldContain("TXN-1001");
    }
}
