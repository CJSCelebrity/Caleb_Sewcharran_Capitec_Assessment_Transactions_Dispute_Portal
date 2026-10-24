using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CapitecDisputes.Application.Authorization;
using Microsoft.IdentityModel.Tokens;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace CapitecDisputes.Tests.TestSupport;

public class DevTokenGenerator(ITestOutputHelper output)
{
    private const string Issuer = "CapitecDisputes";
    private const string Audience = "CapitecDisputesPortal";
    private const string SigningKey = "dev-only-signing-key-change-me-0123456789abcdef";

    [Theory]
    [InlineData(Roles.ReadOnly)]
    [InlineData(Roles.DisputeAdmin)]
    public void Writes_a_bearer_token_for_the_role(string role)
    {
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: [new Claim("sub", $"demo-{role}"), new Claim("role", role)],
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: credentials);

        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        var encoded = handler.WriteToken(token);

        encoded.ShouldNotBeNullOrWhiteSpace();

        var principal = handler.ValidateToken(
            encoded,
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = Issuer,
                ValidAudience = Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)),
                NameClaimType = "sub",
                RoleClaimType = "role"
            },
            out _);

        principal.IsInRole(role).ShouldBeTrue();
        output.WriteLine($"{role}: {encoded}");
    }
}
