using System;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using CapitecDisputes.Application.Authorization;
using CapitecDisputes.Domain.Entities;
using CapitecDisputes.Infrastructure.Security;
using CapitecDisputes.Tests.TestSupport;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Shouldly;

namespace CapitecDisputes.Tests.Security;

public class JwtTokenIssuerTests
{
    private const string SigningKey = "unit-test-signing-key-not-a-real-secret-000000";

    private static readonly JwtOptions Settings = new()
    {
        Issuer = "CapitecDisputes",
        Audience = "CapitecDisputesPortal",
        SigningKey = SigningKey,
        TokenLifetimeHours = 8
    };

    private static readonly DateTimeOffset Now = new(2026, 10, 25, 9, 0, 0, TimeSpan.Zero);

    private static User Admin() =>
        User.Create(
            Guid.Parse("0199c0de-0002-7000-8000-000000000002"),
            "admin@capitec.test",
            "hash",
            Roles.DisputeAdmin,
            null,
            Now);

    private static readonly Guid CustomerId = Guid.Parse("0199c0de-0003-7000-8000-000000000001");

    private static User Customer() =>
        User.Create(
            Guid.Parse("0199c0de-0002-7000-8000-000000000003"),
            "thandi@example.test",
            "hash",
            Roles.Customer,
            CustomerId,
            Now);

    private static JwtTokenIssuer Issuer() =>
        new(Options.Create(Settings), new FixedTimeProvider(Now));

    [Fact]
    public void Issue_stamps_the_expiry_at_the_configured_lifetime()
    {
        Issuer().Issue(Admin()).ExpiresAtUtc.ShouldBe(Now.AddHours(8));
    }

    [Fact]
    public void Issue_mints_a_token_that_satisfies_the_api_validation_parameters()
    {
        var token = Issuer().Issue(Admin());

        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };

        var principal = handler.ValidateToken(
            token.Value,
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = false,
                ValidateIssuerSigningKey = true,
                ValidIssuer = Settings.Issuer,
                ValidAudience = Settings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)),
                NameClaimType = "sub",
                RoleClaimType = "role"
            },
            out _);

        principal.IsInRole(Roles.DisputeAdmin).ShouldBeTrue();
        principal.Identity!.Name.ShouldBe("admin@capitec.test");
    }

    [Fact]
    public void Issue_carries_the_customer_id_as_a_claim()
    {
        var token = Issuer().Issue(Customer());

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token.Value);

        jwt.Claims.ShouldContain(claim => claim.Type == "customerId" && claim.Value == CustomerId.ToString());
    }

    [Fact]
    public void Issue_omits_the_customer_id_claim_for_staff()
    {
        var token = Issuer().Issue(Admin());

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token.Value);

        jwt.Claims.ShouldNotContain(claim => claim.Type == "customerId");
    }
}
