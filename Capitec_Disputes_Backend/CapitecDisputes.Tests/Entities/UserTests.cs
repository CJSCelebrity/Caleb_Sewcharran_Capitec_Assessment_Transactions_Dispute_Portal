using System;
using CapitecDisputes.Application.Authorization;
using CapitecDisputes.Domain.Entities;
using Shouldly;

namespace CapitecDisputes.Tests.Entities;

public class UserTests
{
    [Fact]
    public void Create_normalizes_the_email_to_lowercase_and_trims_it()
    {
        var user = User.Create(
            Guid.Parse("0199c0de-0002-7000-8000-000000000001"),
            "  Admin@Capitec.Test  ",
            "hash",
            Roles.DisputeAdmin,
            new DateTimeOffset(2026, 10, 25, 8, 0, 0, TimeSpan.FromHours(2)));

        user.Email.ShouldBe("admin@capitec.test");
    }

    [Fact]
    public void Create_stores_the_created_timestamp_in_utc()
    {
        var user = User.Create(
            Guid.CreateVersion7(),
            "readonly@capitec.test",
            "hash",
            Roles.ReadOnly,
            new DateTimeOffset(2026, 10, 25, 8, 0, 0, TimeSpan.FromHours(2)));

        user.CreatedAt.Offset.ShouldBe(TimeSpan.Zero);
        user.CreatedAt.Hour.ShouldBe(6);
    }
}
