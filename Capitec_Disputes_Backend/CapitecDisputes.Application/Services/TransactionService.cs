using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CapitecDisputes.Application.Abstractions;
using CapitecDisputes.Application.Authorization;
using CapitecDisputes.Application.Contracts;
using CapitecDisputes.Domain.Entities;

namespace CapitecDisputes.Application.Services;

public sealed class TransactionService(
    ITransactionRepository transactionRepository,
    IDisputeRepository disputeRepository) : ITransactionService
{
    public async Task<IReadOnlyList<TransactionResponse>> GetForCustomerAsync(
        CallerContext caller,
        CancellationToken cancellationToken)
    {
        if (caller.CustomerId is not { } customerId)
        {
            return [];
        }

        var transactions = await transactionRepository.ListForCustomerAsync(customerId, cancellationToken);
        var disputes = await disputeRepository.ListAsync(customerId, null, cancellationToken);

        var latestDisputeByTransaction = disputes
            .GroupBy(dispute => dispute.TransactionId)
            .ToDictionary(
                group => group.Key,
                group => group.OrderByDescending(dispute => dispute.CreatedAt).First());

        return transactions
            .Select(transaction => ToResponse(
                transaction,
                latestDisputeByTransaction.GetValueOrDefault(transaction.Id)))
            .ToList();
    }

    private static TransactionResponse ToResponse(Transaction transaction, Dispute? dispute) =>
        new(
            transaction.Id,
            transaction.Reference,
            transaction.Amount,
            transaction.Currency,
            transaction.OccurredAt,
            transaction.MerchantName,
            dispute?.Id,
            dispute?.Status.ToString());
}
