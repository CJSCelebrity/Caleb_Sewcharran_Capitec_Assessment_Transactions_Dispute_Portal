using System;
using CapitecDisputes.Application.Abstractions;
using Microsoft.AspNetCore.Identity;

namespace CapitecDisputes.Infrastructure.Security;

internal sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private static readonly object HashOwner = new();

    private readonly PasswordHasher<object> hasher = new();

    public string Hash(string password) => hasher.HashPassword(HashOwner, password);

    public bool Verify(string password, string passwordHash)
    {
        try
        {
            return hasher.VerifyHashedPassword(HashOwner, passwordHash, password)
                is PasswordVerificationResult.Success
                or PasswordVerificationResult.SuccessRehashNeeded;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
