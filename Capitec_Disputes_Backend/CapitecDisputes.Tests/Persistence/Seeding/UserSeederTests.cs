using CapitecDisputes.Application.Authorization;
using CapitecDisputes.Infrastructure.Persistence.Seeding;
using Shouldly;

namespace CapitecDisputes.Tests.Persistence.Seeding;

public class UserSeederTests
{
    [Fact]
    public void CustomerIdFor_mints_an_id_for_a_customer()
    {
        UserSeeder.CustomerIdFor(Roles.Customer).ShouldNotBeNull();
    }

    [Fact]
    public void CustomerIdFor_mints_a_distinct_id_each_time()
    {
        UserSeeder.CustomerIdFor(Roles.Customer).ShouldNotBe(UserSeeder.CustomerIdFor(Roles.Customer));
    }

    [Theory]
    [InlineData(Roles.ReadOnly)]
    [InlineData(Roles.DisputeAdmin)]
    public void CustomerIdFor_leaves_staff_without_a_customer_id(string role)
    {
        UserSeeder.CustomerIdFor(role).ShouldBeNull();
    }
}
