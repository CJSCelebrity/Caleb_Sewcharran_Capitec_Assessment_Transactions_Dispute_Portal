using System.Collections.Generic;

namespace CapitecDisputes.Infrastructure.Persistence.Seeding;

public sealed class SeedUserOptions
{
    public const string SectionName = "Seed:Users";

    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;
}

public sealed class SeedUsersOptions
{
    public IList<SeedUserOptions> Users { get; set; } = [];
}
