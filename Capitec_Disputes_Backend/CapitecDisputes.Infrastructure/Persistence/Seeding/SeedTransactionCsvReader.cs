using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace CapitecDisputes.Infrastructure.Persistence.Seeding;

internal static class SeedTransactionCsvReader
{
    private const string ExpectedHeader = "reference,customeremail,amount,currency,occurredat,merchantname";

    private const string ExpectedShape = "reference,customerEmail,amount,currency,occurredAt,merchantName";

    private const int FieldCount = 6;

    public static IReadOnlyList<SeedTransaction> Read(TextReader reader, string source)
    {
        var transactions = new List<SeedTransaction>();
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

            transactions.Add(ParseTransaction(line, source, lineNumber));
        }

        if (!headerSeen)
        {
            throw new InvalidOperationException(
                $"{source} has no rows. The first line must be '{ExpectedShape}'.");
        }

        RequireUniqueReferences(transactions, source);

        return transactions;
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

    private static SeedTransaction ParseTransaction(string line, string source, int lineNumber)
    {
        var fields = line.Split(',', FieldCount);

        if (fields.Length < FieldCount)
        {
            throw new InvalidOperationException(
                $"{source} line {lineNumber}: expected '{ExpectedShape}' but found '{line.Trim()}'.");
        }

        var reference = fields[0].Trim();
        var customerEmail = fields[1].Trim();
        var merchantName = fields[5].Trim();

        if (reference.Length == 0)
        {
            throw new InvalidOperationException($"{source} line {lineNumber}: the reference is blank.");
        }

        if (customerEmail.Length == 0)
        {
            throw new InvalidOperationException(
                $"{source} line {lineNumber}: the customer email for '{reference}' is blank.");
        }

        if (merchantName.Length == 0)
        {
            throw new InvalidOperationException(
                $"{source} line {lineNumber}: the merchant name for '{reference}' is blank.");
        }

        if (!decimal.TryParse(
                fields[2].Trim(),
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var amount))
        {
            throw new InvalidOperationException(
                $"{source} line {lineNumber}: '{reference}' has amount '{fields[2].Trim()}', which is not a number.");
        }

        if (!DateTimeOffset.TryParse(
                fields[4].Trim(),
                CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                out var occurredAt))
        {
            throw new InvalidOperationException(
                $"{source} line {lineNumber}: '{reference}' has date '{fields[4].Trim()}', which is not a date.");
        }

        return new SeedTransaction(
            reference,
            customerEmail,
            amount,
            fields[3].Trim(),
            occurredAt,
            merchantName);
    }

    private static void RequireUniqueReferences(IReadOnlyList<SeedTransaction> transactions, string source)
    {
        var duplicate = transactions
            .GroupBy(transaction => transaction.Reference.ToLowerInvariant())
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicate is not null)
        {
            throw new InvalidOperationException($"{source} lists '{duplicate.Key}' more than once.");
        }
    }
}
