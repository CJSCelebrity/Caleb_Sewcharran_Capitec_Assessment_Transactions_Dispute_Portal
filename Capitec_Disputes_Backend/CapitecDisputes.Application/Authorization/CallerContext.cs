using System;
using System.Security.Claims;

namespace CapitecDisputes.Application.Authorization;

public sealed record CallerContext(string Email, string Role, Guid? CustomerId)
{
    public bool IsCustomer => Role == Roles.Customer;
}

public static class ClaimsPrincipalExtensions
{
    public static CallerContext ToCallerContext(this ClaimsPrincipal principal) =>
        new(
            principal.FindFirst("sub")?.Value ?? string.Empty,
            principal.FindFirst("role")?.Value ?? string.Empty,
            Guid.TryParse(principal.FindFirst("customerId")?.Value, out var customerId) ? customerId : null);
}
