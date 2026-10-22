namespace CapitecDisputes.Infrastructure.Persistence.Seeding;

internal sealed record DemoUser(Guid Id, string Email, string Password, string Role);


internal class DemoUsers()
{
    internal static readonly IReadOnlyList<DemoUser> All =
    [
    ];
}