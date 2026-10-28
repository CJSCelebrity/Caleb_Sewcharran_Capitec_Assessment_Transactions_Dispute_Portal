using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CapitecDisputes.Application.Authorization;
using CapitecDisputes.Application.Contracts;
using CapitecDisputes.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CapitecDisputes.Api.Controllers;

[ApiController]
[Route("api/transactions")]
[Authorize(Roles = Roles.Customer)]
public sealed class TransactionsController(ITransactionService transactionService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<TransactionResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TransactionResponse>>> GetAll(
        CancellationToken cancellationToken) =>
        Ok(await transactionService.GetForCustomerAsync(User.ToCallerContext(), cancellationToken));
}
