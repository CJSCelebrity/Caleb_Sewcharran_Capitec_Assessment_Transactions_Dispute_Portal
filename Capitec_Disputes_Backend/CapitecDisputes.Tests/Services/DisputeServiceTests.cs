using CapitecDisputes.Application.Abstractions;
using CapitecDisputes.Application.Contracts;
using CapitecDisputes.Application.Services;
using CapitecDisputes.Domain.Entities;
using CapitecDisputes.Domain.Exceptions;
using CapitecDisputes.Tests.TestSupport;
using Moq;
using Shouldly;

namespace CapitecDisputes.Tests.Services;

public class DisputeServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 22, 10, 15, 0, TimeSpan.Zero);
    private static readonly Guid MissingId = Guid.Parse("0199c0de-0001-7000-8000-00000000dead");

    private readonly Mock<IDisputeRepository> _repository = new(MockBehavior.Strict);
    private readonly DisputeService _service;

    public DisputeServiceTests() =>
        _service = new DisputeService(_repository.Object, new FixedTimeProvider(Now));

    [Fact]
    public async Task GetAllAsync_passes_the_status_filter_to_the_repository()
    {
        _repository
            .Setup(repository => repository.ListAsync(DisputeStatus.Initiated, It.IsAny<CancellationToken>()))
            .ReturnsAsync([RaiseDispute()]);

        var result = await _service.GetAllAsync(DisputeStatus.Initiated, CancellationToken.None);

        result.Count.ShouldBe(1);
        result[0].Status.ShouldBe("Initiated");
        _repository.Verify(
            repository => repository.ListAsync(DisputeStatus.Initiated, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_passes_a_null_filter_through_unchanged()
    {
        _repository
            .Setup(repository => repository.ListAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _service.GetAllAsync(null, CancellationToken.None);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetByIdAsync_returns_null_when_the_dispute_does_not_exist()
    {
        _repository
            .Setup(repository => repository.FindAsync(MissingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Dispute?)null);

        var result = await _service.GetByIdAsync(MissingId, CancellationToken.None);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetByIdAsync_maps_the_dispute_it_found()
    {
        var dispute = RaiseDispute();
        _repository
            .Setup(repository => repository.FindAsync(dispute.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dispute);

        var result = await _service.GetByIdAsync(dispute.Id, CancellationToken.None);

        result.ShouldNotBeNull();
        result.Id.ShouldBe(dispute.Id);
        result.MerchantName.ShouldBe("Takealot");
        result.Status.ShouldBe("Initiated");
        result.ResolvedAt.ShouldBeNull();
    }

    [Fact]
    public async Task CreateAsync_stores_an_initiated_dispute_stamped_with_the_current_time()
    {
        Dispute? added = null;
        _repository
            .Setup(repository => repository.Add(It.IsAny<Dispute>()))
            .Callback<Dispute>(dispute => added = dispute);
        _repository
            .Setup(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var response = await _service.CreateAsync(
            new CreateDisputeRequest(
                Guid.Parse("0199c0de-1001-7000-8000-000000000001"),
                1499.99m,
                "zar",
                new DateTimeOffset(2026, 10, 12, 9, 14, 0, TimeSpan.Zero),
                "Takealot",
                "Charged twice for a single order."),
            CancellationToken.None);

        response.Status.ShouldBe("Initiated");
        response.CreatedAt.ShouldBe(Now);
        response.TransactionCurrency.ShouldBe("ZAR");
        added.ShouldNotBeNull();
        added.Id.ShouldBe(response.Id);
        _repository.Verify(
            repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ChangeStatusAsync_returns_null_when_the_dispute_does_not_exist()
    {
        _repository
            .Setup(repository => repository.FindAsync(MissingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Dispute?)null);

        var result = await _service.ChangeStatusAsync(
            MissingId, DisputeStatus.UnderInvestigation, CancellationToken.None);

        result.ShouldBeNull();
        _repository.Verify(
            repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ChangeStatusAsync_saves_the_advanced_dispute()
    {
        var dispute = RaiseDispute();
        _repository
            .Setup(repository => repository.FindAsync(dispute.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dispute);
        _repository
            .Setup(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _service.ChangeStatusAsync(
            dispute.Id, DisputeStatus.UnderInvestigation, CancellationToken.None);

        result.ShouldNotBeNull();
        result.Status.ShouldBe("UnderInvestigation");
        _repository.Verify(
            repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ChangeStatusAsync_stamps_resolved_at_from_the_clock()
    {
        var dispute = RaiseDispute();
        dispute.ChangeStatus(DisputeStatus.UnderInvestigation, Now);
        _repository
            .Setup(repository => repository.FindAsync(dispute.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dispute);
        _repository
            .Setup(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _service.ChangeStatusAsync(
            dispute.Id, DisputeStatus.Resolved, CancellationToken.None);

        result.ShouldNotBeNull();
        result.ResolvedAt.ShouldBe(Now);
    }

    [Fact]
    public async Task ChangeStatusAsync_does_not_save_an_invalid_transition()
    {
        var dispute = RaiseDispute();
        _repository
            .Setup(repository => repository.FindAsync(dispute.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dispute);

        await Should.ThrowAsync<InvalidDisputeStatusTransitionException>(
            () => _service.ChangeStatusAsync(dispute.Id, DisputeStatus.Resolved, CancellationToken.None));

        _repository.Verify(
            repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static Dispute RaiseDispute() =>
        Dispute.Raise(
            Guid.Parse("0199c0de-1001-7000-8000-000000000001"),
            1499.99m,
            "ZAR",
            new DateTimeOffset(2026, 10, 12, 9, 14, 0, TimeSpan.Zero),
            "Takealot",
            "Charged twice for a single order.",
            new DateTimeOffset(2026, 10, 13, 7, 30, 0, TimeSpan.Zero));
}
