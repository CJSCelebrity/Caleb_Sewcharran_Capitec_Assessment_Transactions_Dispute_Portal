using System.Threading;
using System.Threading.Tasks;
using CapitecDisputes.Domain.Entities;

namespace CapitecDisputes.Application.Abstractions;

public interface IUserRepository
{
    Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken);
}
