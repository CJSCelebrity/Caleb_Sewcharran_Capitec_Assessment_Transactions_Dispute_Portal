namespace CapitecDisputes.Infrastructure.Persistence.Seeding;

public sealed class SeedOptions
{
    public const string DefaultUsersFileName = "seed-users.csv";

    public const string DefaultTransactionsFileName = "seed-transactions.csv";

    public const string DefaultDisputesFileName = "seed-disputes.csv";

    public string UsersFilePath { get; set; } = DefaultUsersFileName;

    public string TransactionsFilePath { get; set; } = DefaultTransactionsFileName;

    public string DisputesFilePath { get; set; } = DefaultDisputesFileName;
}
