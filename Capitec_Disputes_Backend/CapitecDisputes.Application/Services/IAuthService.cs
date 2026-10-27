using System.Threading;
using System.Threading.Tasks;
using CapitecDisputes.Application.Contracts;

namespace CapitecDisputes.Application.Services;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
}
