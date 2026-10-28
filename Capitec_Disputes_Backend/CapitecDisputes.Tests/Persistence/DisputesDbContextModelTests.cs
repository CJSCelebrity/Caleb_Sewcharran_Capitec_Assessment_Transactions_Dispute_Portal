using System.Linq;
using CapitecDisputes.Domain.Entities;
using CapitecDisputes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Shouldly;

namespace CapitecDisputes.Tests.Persistence;

public class DisputesDbContextModelTests
{
    private static DisputesDbContext Context() =>
        new(new DbContextOptionsBuilder<DisputesDbContext>()
            .UseNpgsql("Host=localhost;Database=model-only;Username=none;Password=none")
            .Options);

    [Theory]
    [InlineData(typeof(Dispute))]
    [InlineData(typeof(DisputeStatusChange))]
    [InlineData(typeof(Transaction))]
    [InlineData(typeof(User))]
    public void Every_key_is_assigned_by_the_domain_rather_than_the_store(Type entityType)
    {
        using var context = Context();

        var key = context.Model.FindEntityType(entityType)!.FindPrimaryKey()!.Properties.ShouldHaveSingleItem();

        key.ValueGenerated.ShouldBe(
            ValueGenerated.Never,
            $"{entityType.Name}.{key.Name} must be ValueGeneratedNever. A store-generated Guid key makes EF "
            + "treat an appended child with a set key as an existing row, which turns an insert into an update.");
    }

    [Fact]
    public void A_dispute_owns_its_status_changes_with_a_cascading_delete()
    {
        using var context = Context();

        var navigation = context.Model
            .FindEntityType(typeof(Dispute))!
            .FindNavigation(nameof(Dispute.StatusChanges))!;

        navigation.ForeignKey.DeleteBehavior.ShouldBe(DeleteBehavior.Cascade);
        navigation.GetPropertyAccessMode().ShouldBe(PropertyAccessMode.Field);
    }

    [Fact]
    public void A_transaction_reference_is_unique()
    {
        using var context = Context();

        var index = context.Model
            .FindEntityType(typeof(Transaction))!
            .GetIndexes()
            .SingleOrDefault(candidate =>
                candidate.Properties.Count == 1
                && candidate.Properties[0].Name == nameof(Transaction.Reference));

        index.ShouldNotBeNull();
        index.IsUnique.ShouldBeTrue();
    }

    [Fact]
    public void Disputes_are_indexed_by_their_owner()
    {
        using var context = Context();

        context.Model
            .FindEntityType(typeof(Dispute))!
            .GetIndexes()
            .ShouldContain(index =>
                index.Properties.Count == 1
                && index.Properties[0].Name == nameof(Dispute.CustomerId));
    }
}
