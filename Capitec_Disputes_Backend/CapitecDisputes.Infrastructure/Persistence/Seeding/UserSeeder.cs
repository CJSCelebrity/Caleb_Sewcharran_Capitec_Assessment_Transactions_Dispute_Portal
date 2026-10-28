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
    internal static Guid? CustomerIdFor(string role) =>
        role == Roles.Customer ? Guid.CreateVersion7() : null;

    public async Task SeedAsync(
        IReadOnlyList<SeedUser> seedUsers,
        string source,
        CancellationToken cancellationToken)
    {
        if (seedUsers.Count == 0)
        {
            logger.LogWarning(
                "{Source} lists no accounts, so no portal accounts were created.",
                source);

            return;
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
                CustomerIdFor(seedUser.Role),
                timeProvider.GetUtcNow()))
            .ToList();

        if (missing.Count == 0)
        {
            return;
        }

        dbContext.Users.AddRange(missing);

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Seeded {Count} portal account(s) from {Source}.", missing.Count, source);
    }
}
