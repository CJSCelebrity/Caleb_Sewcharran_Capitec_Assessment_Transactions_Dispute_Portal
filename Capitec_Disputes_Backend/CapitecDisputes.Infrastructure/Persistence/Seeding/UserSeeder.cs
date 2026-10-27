using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CapitecDisputes.Application.Abstractions;
using CapitecDisputes.Application.Authorization;
using CapitecDisputes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CapitecDisputes.Infrastructure.Persistence.Seeding;

internal sealed class UserSeeder(
    DisputesDbContext dbContext,
    IPasswordHasher passwordHasher,
    TimeProvider timeProvider,
    ILogger<UserSeeder> logger)
{
    private static readonly string[] KnownRoles = [Roles.ReadOnly, Roles.DisputeAdmin];

    public async Task SeedAsync(IReadOnlyList<SeedUserOptions> seedUsers, CancellationToken cancellationToken)
    {
        if (seedUsers.Count == 0)
        {
            logger.LogWarning(
                "No seed users are configured, so no portal accounts were created. Set the {Section} configuration section to sign in.",
                SeedUserOptions.SectionName);

            return;
        }

        foreach (var seedUser in seedUsers)
        {
            Validate(seedUser);
        }

        var existingEmails = await dbContext.Users
            .Select(user => user.Email)
            .ToListAsync(cancellationToken);

        var missing = seedUsers
            .Where(seedUser => !existingEmails.Contains(seedUser.Email.Trim().ToLowerInvariant()))
            .Select(seedUser => User.Create(
                Guid.CreateVersion7(),
                seedUser.Email,
                passwordHasher.Hash(seedUser.Password),
                seedUser.Role,
                timeProvider.GetUtcNow()))
            .ToList();

        if (missing.Count == 0)
        {
            return;
        }

        dbContext.Users.AddRange(missing);

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Seeded {Count} portal account(s).", missing.Count);
    }

    private static void Validate(SeedUserOptions seedUser)
    {
        if (string.IsNullOrWhiteSpace(seedUser.Email))
        {
            throw new InvalidOperationException($"A {SeedUserOptions.SectionName} entry is missing an Email.");
        }

        if (string.IsNullOrWhiteSpace(seedUser.Password))
        {
            throw new InvalidOperationException(
                $"The {SeedUserOptions.SectionName} entry for '{seedUser.Email}' is missing a Password.");
        }

        if (!KnownRoles.Contains(seedUser.Role))
        {
            throw new InvalidOperationException(
                $"The {SeedUserOptions.SectionName} entry for '{seedUser.Email}' has role '{seedUser.Role}'. "
                + $"Expected one of: {string.Join(", ", KnownRoles)}.");
        }
    }
}
