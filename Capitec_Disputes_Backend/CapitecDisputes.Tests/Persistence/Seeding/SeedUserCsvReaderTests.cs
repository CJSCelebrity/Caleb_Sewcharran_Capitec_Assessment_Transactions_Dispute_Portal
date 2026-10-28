using CapitecDisputes.Infrastructure.Persistence.Seeding;
using Shouldly;

namespace CapitecDisputes.Tests.Persistence.Seeding;

public class SeedUserCsvReaderTests
{
    private static IReadOnlyList<SeedUser> Read(string csv) =>
        SeedUserCsvReader.Read(new StringReader(csv), "seed-users.csv");

    [Fact]
    public void Read_returns_a_user_for_each_data_row()
    {
        var users = Read(
            """
            email,password,role
            admin@capitec.test,DisputeAdmin#2026,DisputeAdmin
            readonly@capitec.test,ReadOnly#2026,ReadOnly
            """);

        users.Count.ShouldBe(2);
        users[0].ShouldBe(new SeedUser("admin@capitec.test", "DisputeAdmin#2026", "DisputeAdmin"));
        users[1].ShouldBe(new SeedUser("readonly@capitec.test", "ReadOnly#2026", "ReadOnly"));
    }

    [Fact]
    public void Read_trims_surrounding_whitespace_on_every_field()
    {
        var users = Read(
            """
            email,password,role
              admin@capitec.test , DisputeAdmin#2026 , DisputeAdmin
            """);

        users[0].ShouldBe(new SeedUser("admin@capitec.test", "DisputeAdmin#2026", "DisputeAdmin"));
    }

    [Fact]
    public void Read_keeps_a_password_that_contains_commas()
    {
        var users = Read(
            """
            email,password,role
            admin@capitec.test,one,two,three,DisputeAdmin
            """);

        users[0].Password.ShouldBe("one,two,three");
    }

    [Fact]
    public void Read_skips_blank_lines()
    {
        var users = Read(
            """
            email,password,role

            admin@capitec.test,DisputeAdmin#2026,DisputeAdmin

            """);

        users.Count.ShouldBe(1);
    }

    [Fact]
    public void Read_returns_nothing_when_the_file_holds_only_a_header()
    {
        Read("email,password,role").ShouldBeEmpty();
    }

    [Fact]
    public void Read_throws_when_the_header_row_is_missing()
    {
        var error = Should.Throw<InvalidOperationException>(() => Read(
            """
            admin@capitec.test,DisputeAdmin#2026,DisputeAdmin
            """));

        error.Message.ShouldContain("email,password,role");
    }

    [Fact]
    public void Read_throws_when_the_file_is_empty()
    {
        Should.Throw<InvalidOperationException>(() => Read(string.Empty));
    }

    [Fact]
    public void Read_throws_with_the_line_number_when_a_row_is_missing_a_field()
    {
        var error = Should.Throw<InvalidOperationException>(() => Read(
            """
            email,password,role
            admin@capitec.test,DisputeAdmin#2026
            """));

        error.Message.ShouldContain("line 2");
    }

    [Fact]
    public void Read_throws_when_the_role_is_not_recognised()
    {
        var error = Should.Throw<InvalidOperationException>(() => Read(
            """
            email,password,role
            admin@capitec.test,DisputeAdmin#2026,Administrator
            """));

        error.Message.ShouldContain("DisputeAdmin");
        error.Message.ShouldContain("ReadOnly");
    }

    [Fact]
    public void Read_throws_when_the_password_is_blank()
    {
        var error = Should.Throw<InvalidOperationException>(() => Read(
            """
            email,password,role
            admin@capitec.test,,DisputeAdmin
            """));

        error.Message.ShouldContain("admin@capitec.test");
    }

    [Fact]
    public void Read_throws_when_the_email_is_blank()
    {
        Should.Throw<InvalidOperationException>(() => Read(
            """
            email,password,role
            ,DisputeAdmin#2026,DisputeAdmin
            """));
    }

    [Fact]
    public void Read_throws_when_an_email_appears_twice()
    {
        var error = Should.Throw<InvalidOperationException>(() => Read(
            """
            email,password,role
            admin@capitec.test,DisputeAdmin#2026,DisputeAdmin
            ADMIN@capitec.test,Another#2026,ReadOnly
            """));

        error.Message.ShouldContain("admin@capitec.test");
    }

    [Fact]
    public void Read_accepts_the_customer_role()
    {
        var users = Read(
            """
            email,password,role
            thandi@example.test,Customer#2026,Customer
            """);

        users[0].ShouldBe(new SeedUser("thandi@example.test", "Customer#2026", "Customer"));
    }
}
