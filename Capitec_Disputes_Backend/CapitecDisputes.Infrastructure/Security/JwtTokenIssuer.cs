using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CapitecDisputes.Application.Abstractions;
using CapitecDisputes.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CapitecDisputes.Infrastructure.Security;

internal sealed class JwtTokenIssuer(IOptions<JwtOptions> options, TimeProvider timeProvider) : ITokenIssuer
{
    public AccessToken Issue(User user)
    {
        var settings = options.Value;
        var expiresAt = timeProvider.GetUtcNow().AddHours(settings.TokenLifetimeHours);

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: settings.Issuer,
            audience: settings.Audience,
            claims: [new Claim("sub", user.Email), new Claim("role", user.Role)],
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new AccessToken(
            new JwtSecurityTokenHandler().WriteToken(token),
            expiresAt);
    }
}
