namespace CapitecDisputes.Domain.Entities;

public sealed class User
{
    private User()
    {
    }

    public Guid Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string Role { get; private set; } = string.Empty;
    public Guid? CustomerId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static User Create(
        Guid id,
        string email,
        string passwordHash,
        string role,
        Guid? customerId,
        DateTimeOffset createdAt) =>
        new()
        {
            Id = id,
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = passwordHash,
            Role = role,
            CustomerId = customerId,
            CreatedAt = createdAt.ToUniversalTime()
        };
}
