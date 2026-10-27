using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CapitecDisputes.Application.Authorization;

namespace CapitecDisputes.Infrastructure.Persistence.Seeding;

internal static class SeedUserCsvReader
{
    private const string ExpectedHeader = "email,password,role";

    private static readonly string[] KnownRoles = [Roles.ReadOnly, Roles.DisputeAdmin];

    public static IReadOnlyList<SeedUser> Read(TextReader reader, string source)
    {
        var users = new List<SeedUser>();
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

            users.Add(ParseUser(line, source, lineNumber));
        }

        if (!headerSeen)
        {
            throw new InvalidOperationException(
                $"{source} has no rows. The first line must be '{ExpectedHeader}'.");
        }

        RequireUniqueEmails(users, source);

        return users;
    }

    private static void RequireHeader(string line, string source, int lineNumber)
    {
        var header = string.Join(',', line.Split(',').Select(field => field.Trim().ToLowerInvariant()));

        if (header != ExpectedHeader)
        {
            throw new InvalidOperationException(
                $"{source} line {lineNumber}: expected the header '{ExpectedHeader}' but found '{line.Trim()}'.");
        }
    }

    private static SeedUser ParseUser(string line, string source, int lineNumber)
    {
        var firstComma = line.IndexOf(',');
        var lastComma = line.LastIndexOf(',');

        if (firstComma < 0 || firstComma == lastComma)
        {
            throw new InvalidOperationException(
                $"{source} line {lineNumber}: expected 'email,password,role' but found '{line.Trim()}'.");
        }

        var email = line[..firstComma].Trim();
        var password = line[(firstComma + 1)..lastComma].Trim();
        var role = line[(lastComma + 1)..].Trim();

        if (email.Length == 0)
        {
            throw new InvalidOperationException($"{source} line {lineNumber}: the email is blank.");
        }

        if (password.Length == 0)
        {
            throw new InvalidOperationException(
                $"{source} line {lineNumber}: the password for '{email}' is blank.");
        }

        if (!KnownRoles.Contains(role))
        {
            throw new InvalidOperationException(
                $"{source} line {lineNumber}: '{email}' has role '{role}'. "
                + $"Expected one of: {string.Join(", ", KnownRoles)}.");
        }

        return new SeedUser(email, password, role);
    }

    private static void RequireUniqueEmails(IReadOnlyList<SeedUser> users, string source)
    {
        var duplicate = users
            .GroupBy(user => user.Email.ToLowerInvariant())
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicate is not null)
        {
            throw new InvalidOperationException($"{source} lists '{duplicate.Key}' more than once.");
        }
    }
}
