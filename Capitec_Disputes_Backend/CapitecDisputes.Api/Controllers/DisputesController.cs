using CapitecDisputes.Application.Authorization;
using CapitecDisputes.Application.Contracts;
using CapitecDisputes.Application.Services;
using CapitecDisputes.Domain.Entities;
using CapitecDisputes.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CapitecDisputes.Api.Controllers;

[ApiController]
[Route("api/disputes")]
[Authorize]
public sealed class DisputesController(IDisputeService disputeService) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = $"{Roles.ReadOnly},{Roles.DisputeAdmin}")]
    [ProducesResponseType<IReadOnlyList<DisputeResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DisputeResponse>>> GetAll(
        [FromQuery] DisputeStatus? status,
        CancellationToken cancellationToken) =>
        Ok(await disputeService.GetAllAsync(status, cancellationToken));

    [HttpGet("{id:guid}")]
    [Authorize(Roles = $"{Roles.ReadOnly},{Roles.DisputeAdmin}")]
    [ProducesResponseType<DisputeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DisputeResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var dispute = await disputeService.GetByIdAsync(id, cancellationToken);

        return dispute is null ? NotFound() : Ok(dispute);
    }

    [HttpPost]
    [Authorize(Roles = Roles.DisputeAdmin)]
    [ProducesResponseType<DisputeResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DisputeResponse>> Create(
        CreateDisputeRequest request,
        CancellationToken cancellationToken)
    {
        var dispute = await disputeService.CreateAsync(request, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = dispute.Id }, dispute);
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
            var dispute = await disputeService.ChangeStatusAsync(id, request.Status!.Value, cancellationToken);

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
