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
    [Required][Range(0.01, 1_000_000_000)] decimal? TransactionAmount,
    [Required][StringLength(3, MinimumLength = 3)] string TransactionCurrency,
    [Required] DateTimeOffset? TransactionDate,
    [Required][StringLength(200, MinimumLength = 1)] string MerchantName,
    [Required][StringLength(500, MinimumLength = 1)] string Reason);

public sealed record UpdateDisputeStatusRequest(
    [Required][EnumDataType(typeof(DisputeStatus))] DisputeStatus? Status);
