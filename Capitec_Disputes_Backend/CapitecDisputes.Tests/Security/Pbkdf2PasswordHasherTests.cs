using CapitecDisputes.Infrastructure.Security;
using Shouldly;

namespace CapitecDisputes.Tests.Security;

public class Pbkdf2PasswordHasherTests
{
    private readonly Pbkdf2PasswordHasher hasher = new();

    [Fact]
    public void Verify_accepts_the_password_that_produced_the_hash()
    {
        var hash = hasher.Hash("DisputeAdmin#2026");

        hasher.Verify("DisputeAdmin#2026", hash).ShouldBeTrue();
    }

    [Fact]
    public void Verify_rejects_a_different_password()
    {
        var hash = hasher.Hash("DisputeAdmin#2026");

        hasher.Verify("wrong-password", hash).ShouldBeFalse();
    }

    [Fact]
    public void Hash_produces_a_different_value_each_time_for_the_same_password()
    {
        hasher.Hash("same").ShouldNotBe(hasher.Hash("same"));
    }

    [Fact]
    public void Verify_rejects_a_malformed_hash_instead_of_throwing()
    {
        hasher.Verify("anything", "not-a-real-hash").ShouldBeFalse();
    }
}
