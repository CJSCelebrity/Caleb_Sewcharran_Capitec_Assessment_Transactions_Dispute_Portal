using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CapitecDisputes.Application.Authorization;
using CapitecDisputes.Application.Contracts;

namespace CapitecDisputes.Application.Services;

public interface ITransactionService
{
    Task<IReadOnlyList<TransactionResponse>> GetForCustomerAsync(
        CallerContext caller,
        CancellationToken cancellationToken);
}
