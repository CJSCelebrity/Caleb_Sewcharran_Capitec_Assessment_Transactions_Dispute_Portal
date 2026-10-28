using System;
using System.Security.Claims;
using CapitecDisputes.Application.Authorization;
using Shouldly;

namespace CapitecDisputes.Tests.Authorization;

public class CallerContextTests
{
    private static ClaimsPrincipal PrincipalWith(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, "Test", "sub", "role"));

    [Fact]
    public void ToCallerContext_reads_a_customer_identity()
    {
        var customerId = Guid.CreateVersion7();

        var caller = PrincipalWith(
                new Claim("sub", "thandi@example.test"),
                new Claim("role", Roles.Customer),
                new Claim("customerId", customerId.ToString()))
            .ToCallerContext();

        caller.Email.ShouldBe("thandi@example.test");
        caller.Role.ShouldBe(Roles.Customer);
        caller.CustomerId.ShouldBe(customerId);
        caller.IsCustomer.ShouldBeTrue();
    }

    [Fact]
    public void ToCallerContext_leaves_the_customer_id_null_for_staff()
    {
        var caller = PrincipalWith(
                new Claim("sub", "admin@capitec.test"),
                new Claim("role", Roles.DisputeAdmin))
            .ToCallerContext();

        caller.CustomerId.ShouldBeNull();
        caller.IsCustomer.ShouldBeFalse();
    }

    [Fact]
    public void ToCallerContext_ignores_an_unparseable_customer_id()
    {
        var caller = PrincipalWith(
                new Claim("sub", "thandi@example.test"),
                new Claim("role", Roles.Customer),
                new Claim("customerId", "not-a-guid"))
            .ToCallerContext();

        caller.CustomerId.ShouldBeNull();
    }

    [Fact]
    public void ToCallerContext_falls_back_to_empty_strings_when_the_claims_are_absent()
    {
        var caller = PrincipalWith().ToCallerContext();

        caller.Email.ShouldBe(string.Empty);
        caller.Role.ShouldBe(string.Empty);
        caller.CustomerId.ShouldBeNull();
        caller.IsCustomer.ShouldBeFalse();
    }
}
