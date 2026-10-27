namespace CapitecDisputes.Infrastructure.Persistence.Seeding;

public sealed class SeedOptions
{
    public const string DefaultUsersFileName = "seed-users.csv";

    public string UsersFilePath { get; set; } = DefaultUsersFileName;
}
