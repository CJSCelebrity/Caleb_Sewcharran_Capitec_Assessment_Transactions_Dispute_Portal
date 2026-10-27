using System;
using CapitecDisputes.Domain.Entities;

namespace CapitecDisputes.Application.Abstractions;

public sealed record AccessToken(string Value, DateTimeOffset ExpiresAtUtc);

public interface ITokenIssuer
{
    AccessToken Issue(User user);
}
