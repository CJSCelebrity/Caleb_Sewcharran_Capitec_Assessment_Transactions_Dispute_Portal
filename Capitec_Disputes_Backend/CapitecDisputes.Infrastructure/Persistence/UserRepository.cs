using System.Threading;
using System.Threading.Tasks;
using CapitecDisputes.Application.Abstractions;
using CapitecDisputes.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CapitecDisputes.Infrastructure.Persistence;

internal sealed class UserRepository(DisputesDbContext dbContext) : IUserRepository
{
    public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var normalized = email.Trim().ToLowerInvariant();

        return dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(user => user.Email == normalized, cancellationToken);
    }
}
