using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CapitecDisputes.Domain.Entities;

namespace CapitecDisputes.Infrastructure.Persistence.Seeding;

internal static class SeedDisputeCsvReader
{
    private const string ExpectedHeader = "transactionreference,status,reason";

    private const string ExpectedShape = "transactionReference,status,reason";

    private const int FieldCount = 3;

    public static IReadOnlyList<SeedDispute> Read(TextReader reader, string source)
    {
        var disputes = new List<SeedDispute>();
        var lineNumber = 0;
        var headerSeen = false;

        while (reader.ReadLine() is { } line)
        {
            lineNumber++;

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (!headerSeen)
            {
                RequireHeader(line, source, lineNumber);
                headerSeen = true;

                continue;
            }

            disputes.Add(ParseDispute(line, source, lineNumber));
        }

        if (!headerSeen)
        {
            throw new InvalidOperationException(
                $"{source} has no rows. The first line must be '{ExpectedShape}'.");
        }

        RequireUniqueTransactions(disputes, source);

        return disputes;
    }

    private static void RequireHeader(string line, string source, int lineNumber)
    {
        var header = string.Join(',', line.Split(',').Select(field => field.Trim().ToLowerInvariant()));

        if (header != ExpectedHeader)
        {
            throw new InvalidOperationException(
                $"{source} line {lineNumber}: expected the header '{ExpectedShape}' but found '{line.Trim()}'.");
        }
    }

    private static SeedDispute ParseDispute(string line, string source, int lineNumber)
    {
        var fields = line.Split(',', FieldCount);

        if (fields.Length < FieldCount)
        {
            throw new InvalidOperationException(
                $"{source} line {lineNumber}: expected '{ExpectedShape}' but found '{line.Trim()}'.");
        }

        var reference = fields[0].Trim();
        var status = fields[1].Trim();
        var reason = fields[2].Trim();

        if (reference.Length == 0)
        {
            throw new InvalidOperationException(
                $"{source} line {lineNumber}: the transaction reference is blank.");
        }

        if (reason.Length == 0)
        {
            throw new InvalidOperationException(
                $"{source} line {lineNumber}: the reason for '{reference}' is blank.");
        }

        if (!Enum.TryParse<DisputeStatus>(status, out var parsedStatus) || !Enum.IsDefined(parsedStatus))
        {
            throw new InvalidOperationException(
                $"{source} line {lineNumber}: '{reference}' has status '{status}'. "
                + $"Expected one of: {string.Join(", ", Enum.GetNames<DisputeStatus>())}.");
        }

        return new SeedDispute(reference, parsedStatus, reason);
    }

    private static void RequireUniqueTransactions(IReadOnlyList<SeedDispute> disputes, string source)
    {
        var duplicate = disputes
            .GroupBy(dispute => dispute.TransactionReference.ToLowerInvariant())
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicate is not null)
        {
            throw new InvalidOperationException($"{source} lists '{duplicate.Key}' more than once.");
        }
    }
}
