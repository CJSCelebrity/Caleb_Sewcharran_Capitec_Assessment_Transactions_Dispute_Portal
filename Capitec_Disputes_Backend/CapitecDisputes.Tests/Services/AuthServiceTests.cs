using System;
using System.Threading;
using System.Threading.Tasks;
using CapitecDisputes.Application.Abstractions;
using CapitecDisputes.Application.Authorization;
using CapitecDisputes.Application.Contracts;
using CapitecDisputes.Application.Services;
using CapitecDisputes.Domain.Entities;
using Moq;
using Shouldly;

namespace CapitecDisputes.Tests.Services;

public class AuthServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 25, 9, 0, 0, TimeSpan.Zero);

    private readonly Mock<IUserRepository> users = new(MockBehavior.Strict);
    private readonly Mock<IPasswordHasher> hasher = new(MockBehavior.Strict);
    private readonly Mock<ITokenIssuer> issuer = new(MockBehavior.Strict);

    private AuthService Service() => new(users.Object, hasher.Object, issuer.Object);

    private static User Admin() =>
        User.Create(
            Guid.Parse("0199c0de-0002-7000-8000-000000000002"),
            "admin@capitec.test",
            "stored-hash",
            Roles.DisputeAdmin,
            null,
            Now);

    private static readonly Guid CustomerId = Guid.Parse("0199c0de-0003-7000-8000-000000000001");

    private static User Customer() =>
        User.Create(
            Guid.Parse("0199c0de-0002-7000-8000-000000000004"),
            "thandi@example.test",
            "stored-hash",
            Roles.Customer,
            CustomerId,
            Now);

    [Fact]
    public async Task LoginAsync_returns_null_when_the_email_is_unknown()
    {
        users
            .Setup(repository => repository.FindByEmailAsync("nobody@capitec.test", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var result = await Service().LoginAsync(
            new LoginRequest("nobody@capitec.test", "whatever"),
            CancellationToken.None);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task LoginAsync_returns_null_when_the_password_does_not_match()
    {
        users
            .Setup(repository => repository.FindByEmailAsync("admin@capitec.test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Admin());
        hasher.Setup(instance => instance.Verify("wrong", "stored-hash")).Returns(false);

        var result = await Service().LoginAsync(
            new LoginRequest("admin@capitec.test", "wrong"),
            CancellationToken.None);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task LoginAsync_returns_the_token_email_and_role_on_success()
    {
        users
            .Setup(repository => repository.FindByEmailAsync("admin@capitec.test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Admin());
        hasher.Setup(instance => instance.Verify("DisputeAdmin#2026", "stored-hash")).Returns(true);
        issuer
            .Setup(instance => instance.Issue(It.Is<User>(user => user.Email == "admin@capitec.test")))
            .Returns(new AccessToken("signed-token", Now.AddHours(8)));

        var result = await Service().LoginAsync(
            new LoginRequest("admin@capitec.test", "DisputeAdmin#2026"),
            CancellationToken.None);

        result.ShouldNotBeNull();
        result.AccessToken.ShouldBe("signed-token");
        result.ExpiresAtUtc.ShouldBe(Now.AddHours(8));
        result.Email.ShouldBe("admin@capitec.test");
        result.Role.ShouldBe(Roles.DisputeAdmin);
        result.CustomerId.ShouldBeNull();
    }

    [Fact]
    public async Task LoginAsync_does_not_issue_a_token_when_verification_fails()
    {
        users
            .Setup(repository => repository.FindByEmailAsync("admin@capitec.test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Admin());
        hasher.Setup(instance => instance.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

        await Service().LoginAsync(new LoginRequest("admin@capitec.test", "wrong"), CancellationToken.None);

        issuer.Verify(instance => instance.Issue(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_returns_the_customer_id_for_a_customer()
    {
        users
            .Setup(repository => repository.FindByEmailAsync("thandi@example.test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Customer());
        hasher.Setup(instance => instance.Verify("Customer#2026", "stored-hash")).Returns(true);
        issuer
            .Setup(instance => instance.Issue(It.Is<User>(user => user.Email == "thandi@example.test")))
            .Returns(new AccessToken("signed-token", Now.AddHours(8)));

        var result = await Service().LoginAsync(
            new LoginRequest("thandi@example.test", "Customer#2026"),
            CancellationToken.None);

        result.ShouldNotBeNull();
        result.Role.ShouldBe(Roles.Customer);
        result.CustomerId.ShouldBe(CustomerId);
    }
}
