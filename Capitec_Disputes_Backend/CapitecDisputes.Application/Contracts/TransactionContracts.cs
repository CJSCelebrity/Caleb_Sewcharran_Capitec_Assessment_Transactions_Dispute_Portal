using System;

namespace CapitecDisputes.Application.Contracts;

public sealed record TransactionResponse(
    Guid Id,
    string Reference,
    decimal Amount,
    string Currency,
    DateTimeOffset OccurredAt,
    string MerchantName,
    Guid? DisputeId,
    string? DisputeStatus);
