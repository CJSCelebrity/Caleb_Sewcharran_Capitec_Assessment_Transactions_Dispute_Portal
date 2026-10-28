using System;
using System.ComponentModel.DataAnnotations;
using CapitecDisputes.Domain.Entities;

namespace CapitecDisputes.Application.Contracts;

public sealed record DisputeResponse(
    Guid Id,
    Guid TransactionId,
    decimal TransactionAmount,
    string TransactionCurrency,
    DateTimeOffset TransactionDate,
    string MerchantName,
    string Reason,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ResolvedAt);

public sealed record CreateDisputeRequest(
    [Required] Guid? TransactionId,
    [Required][StringLength(500, MinimumLength = 1)] string Reason);

public sealed record UpdateDisputeStatusRequest(
    [Required][EnumDataType(typeof(DisputeStatus))] DisputeStatus? Status);

public sealed record DisputeStatusChangeResponse(
    string? FromStatus,
    string ToStatus,
    string ChangedBy,
    DateTimeOffset ChangedAt);
