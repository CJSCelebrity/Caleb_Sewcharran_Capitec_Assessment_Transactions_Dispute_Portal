using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CapitecDisputes.Application.Authorization;
using CapitecDisputes.Application.Contracts;
using CapitecDisputes.Application.Services;
using CapitecDisputes.Domain.Entities;
using CapitecDisputes.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CapitecDisputes.Api.Controllers;

[ApiController]
[Route("api/disputes")]
[Authorize]
public sealed class DisputesController(IDisputeService disputeService) : ControllerBase
{
    private const string AnyPortalRole = $"{Roles.Customer},{Roles.ReadOnly},{Roles.DisputeAdmin}";

    [HttpGet]
    [Authorize(Roles = AnyPortalRole)]
    [ProducesResponseType<IReadOnlyList<DisputeResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DisputeResponse>>> GetAll(
        [FromQuery] DisputeStatus? status,
        CancellationToken cancellationToken) =>
        Ok(await disputeService.GetAllAsync(User.ToCallerContext(), status, cancellationToken));

    [HttpGet("{id:guid}")]
    [Authorize(Roles = AnyPortalRole)]
    [ProducesResponseType<DisputeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DisputeResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var dispute = await disputeService.GetByIdAsync(User.ToCallerContext(), id, cancellationToken);

        return dispute is null ? NotFound() : Ok(dispute);
    }

    [HttpGet("{id:guid}/history")]
    [Authorize(Roles = AnyPortalRole)]
    [ProducesResponseType<IReadOnlyList<DisputeStatusChangeResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<DisputeStatusChangeResponse>>> GetHistory(
        Guid id,
        CancellationToken cancellationToken)
    {
        var history = await disputeService.GetHistoryAsync(User.ToCallerContext(), id, cancellationToken);

        return history is null ? NotFound() : Ok(history);
    }

    [HttpPost]
    [Authorize(Roles = Roles.Customer)]
    [ProducesResponseType<DisputeResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DisputeResponse>> Create(
        CreateDisputeRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var dispute = await disputeService.CreateAsync(User.ToCallerContext(), request, cancellationToken);

            return dispute is null
                ? NotFound()
                : CreatedAtAction(nameof(GetById), new { id = dispute.Id }, dispute);
        }
        catch (DisputeAlreadyRaisedException exception)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Transaction already disputed",
                detail: exception.Message);
        }
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = Roles.DisputeAdmin)]
    [ProducesResponseType<DisputeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DisputeResponse>> ChangeStatus(
        Guid id,
        UpdateDisputeStatusRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var dispute = await disputeService.ChangeStatusAsync(
                User.ToCallerContext(),
                id,
                request.Status!.Value,
                cancellationToken);

            return dispute is null ? NotFound() : Ok(dispute);
        }
        catch (InvalidDisputeStatusTransitionException exception)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Invalid dispute status transition",
                detail: exception.Message);
        }
    }
}
