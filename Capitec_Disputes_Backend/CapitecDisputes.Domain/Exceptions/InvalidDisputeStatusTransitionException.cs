using CapitecDisputes.Domain.Entities;

namespace CapitecDisputes.Domain.Exceptions;

public sealed class InvalidDisputeStatusTransitionException(DisputeStatus from, DisputeStatus to)
    : InvalidOperationException($"A dispute cannot move from {from} to {to}.")
{
    public DisputeStatus From { get; } = from;
    public DisputeStatus To { get; } = to;
}
