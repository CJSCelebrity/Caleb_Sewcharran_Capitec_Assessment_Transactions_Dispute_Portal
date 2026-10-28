using CapitecDisputes.Infrastructure.Persistence.Seeding;
using Shouldly;

namespace CapitecDisputes.Tests.Persistence.Seeding;

public class SeedTransactionCsvReaderTests
{
    private const string Header = "reference,customerEmail,amount,currency,occurredAt,merchantName";

    private static IReadOnlyList<SeedTransaction> Read(string csv) =>
        SeedTransactionCsvReader.Read(new StringReader(csv), "seed-transactions.csv");

    [Fact]
    public void Read_returns_a_transaction_for_each_data_row()
    {
        var transactions = Read(
            $"""
            {Header}
            TXN-1001,thandi@example.test,1499.99,ZAR,2026-10-02T09:14:00Z,Takealot
            TXN-1002,thandi@example.test,320.00,ZAR,2026-10-05T18:42:00Z,Uber Eats
            """);

        transactions.Count.ShouldBe(2);
        transactions[0].Reference.ShouldBe("TXN-1001");
        transactions[0].CustomerEmail.ShouldBe("thandi@example.test");
        transactions[0].Amount.ShouldBe(1499.99m);
        transactions[0].Currency.ShouldBe("ZAR");
        transactions[0].MerchantName.ShouldBe("Takealot");
        transactions[1].MerchantName.ShouldBe("Uber Eats");
    }

    [Fact]
    public void Read_parses_the_timestamp_as_utc()
    {
        var transactions = Read(
            $"""
            {Header}
            TXN-1001,thandi@example.test,10.00,ZAR,2026-10-02T11:14:00+02:00,Takealot
            """);

        transactions[0].OccurredAt.Offset.ShouldBe(TimeSpan.Zero);
        transactions[0].OccurredAt.Hour.ShouldBe(9);
    }

    [Fact]
    public void Read_trims_surrounding_whitespace_on_every_field()
    {
        var transactions = Read(
            $"""
            {Header}
              TXN-1001 , thandi@example.test , 1499.99 , ZAR , 2026-10-02T09:14:00Z , Takealot
            """);

        transactions[0].Reference.ShouldBe("TXN-1001");
        transactions[0].CustomerEmail.ShouldBe("thandi@example.test");
        transactions[0].Currency.ShouldBe("ZAR");
        transactions[0].MerchantName.ShouldBe("Takealot");
    }

    [Fact]
    public void Read_keeps_a_merchant_name_that_contains_commas()
    {
        var transactions = Read(
            $"""
            {Header}
            TXN-1001,thandi@example.test,10.00,ZAR,2026-10-02T09:14:00Z,Pick n Pay, Claremont, Cape Town
            """);

        transactions[0].MerchantName.ShouldBe("Pick n Pay, Claremont, Cape Town");
    }

    [Fact]
    public void Read_skips_blank_lines()
    {
        var transactions = Read(
            $"""
            {Header}

            TXN-1001,thandi@example.test,10.00,ZAR,2026-10-02T09:14:00Z,Takealot

            """);

        transactions.ShouldHaveSingleItem();
    }

    [Fact]
    public void Read_returns_nothing_for_a_header_only_file()
    {
        Read(Header).ShouldBeEmpty();
    }

    [Fact]
    public void Read_throws_when_the_header_is_wrong()
    {
        var exception = Should.Throw<InvalidOperationException>(() => Read(
            """
            reference,amount
            TXN-1001,10.00
            """));

        exception.Message.ShouldContain("seed-transactions.csv line 1");
    }

    [Fact]
    public void Read_throws_for_an_empty_file()
    {
        Should.Throw<InvalidOperationException>(() => Read(string.Empty))
            .Message.ShouldContain("seed-transactions.csv");
    }

    [Fact]
    public void Read_throws_when_a_row_has_too_few_fields()
    {
        Should.Throw<InvalidOperationException>(() => Read(
            $"""
            {Header}
            TXN-1001,thandi@example.test,10.00
            """))
            .Message.ShouldContain("line 2");
    }

    [Fact]
    public void Read_throws_when_the_amount_is_not_a_number()
    {
        Should.Throw<InvalidOperationException>(() => Read(
            $"""
            {Header}
            TXN-1001,thandi@example.test,plenty,ZAR,2026-10-02T09:14:00Z,Takealot
            """))
            .Message.ShouldContain("line 2");
    }

    [Fact]
    public void Read_throws_when_the_timestamp_is_not_a_date()
    {
        Should.Throw<InvalidOperationException>(() => Read(
            $"""
            {Header}
            TXN-1001,thandi@example.test,10.00,ZAR,someday,Takealot
            """))
            .Message.ShouldContain("line 2");
    }

    [Fact]
    public void Read_throws_when_the_reference_is_blank()
    {
        Should.Throw<InvalidOperationException>(() => Read(
            $"""
            {Header}
            ,thandi@example.test,10.00,ZAR,2026-10-02T09:14:00Z,Takealot
            """))
            .Message.ShouldContain("line 2");
    }

    [Fact]
    public void Read_throws_when_a_reference_is_listed_twice()
    {
        Should.Throw<InvalidOperationException>(() => Read(
            $"""
            {Header}
            TXN-1001,thandi@example.test,10.00,ZAR,2026-10-02T09:14:00Z,Takealot
            TXN-1001,sipho@example.test,20.00,ZAR,2026-10-03T09:14:00Z,Checkers
            """))
            .Message.ShouldContain("TXN-1001");
    }
}
