using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CapitecDisputes.Application.Abstractions;
using CapitecDisputes.Application.Authorization;
using CapitecDisputes.Application.Contracts;
using CapitecDisputes.Application.Services;
using CapitecDisputes.Domain.Entities;
using CapitecDisputes.Domain.Exceptions;
using CapitecDisputes.Tests.TestSupport;
using Moq;
using Shouldly;
using Xunit;

namespace CapitecDisputes.Tests.Services;

public class DisputeServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 22, 10, 15, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset OccurredAt = new(2026, 10, 12, 9, 14, 0, TimeSpan.Zero);
    private static readonly Guid MissingId = Guid.Parse("0199c0de-0001-7000-8000-00000000dead");
    private static readonly Guid CustomerId = Guid.Parse("0199c0de-0003-7000-8000-000000000001");
    private static readonly Guid OtherCustomerId = Guid.Parse("0199c0de-0003-7000-8000-000000000002");

    private const string CustomerEmail = "thandi@example.test";
    private const string AdminEmail = "admin@capitec.test";
    private const string Reason = "Charged twice for a single order.";

    private readonly Mock<IDisputeRepository> _repository = new(MockBehavior.Strict);
    private readonly Mock<ITransactionRepository> _transactions = new(MockBehavior.Strict);
    private readonly DisputeService _service;

    public DisputeServiceTests() =>
        _service = new DisputeService(_repository.Object, _transactions.Object, new FixedTimeProvider(Now));

    private static CallerContext Customer() => new(CustomerEmail, Roles.Customer, CustomerId);

    private static CallerContext Admin() => new(AdminEmail, Roles.DisputeAdmin, null);

    private static Transaction TransactionFor(Guid customerId, string currency = "ZAR") =>
        Transaction.Record("TXN-1001", customerId, 1499.99m, currency, OccurredAt, "Takealot");

    private static Dispute DisputeFor(Guid customerId) =>
        Dispute.Raise(
            TransactionFor(customerId),
            Reason,
            CustomerEmail,
            new DateTimeOffset(2026, 10, 13, 7, 30, 0, TimeSpan.Zero));

    private void ExpectSave() =>
        _repository
            .Setup(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

    private void ExpectFind(Guid id, Dispute? dispute) =>
        _repository
            .Setup(repository => repository.FindAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dispute);

    private void ExpectFindWithHistory(Guid id, Dispute? dispute) =>
        _repository
            .Setup(repository => repository.FindWithHistoryAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dispute);

    private void ExpectTransaction(Transaction transaction) =>
        _transactions
            .Setup(repository => repository.FindForCustomerAsync(
                transaction.Id,
                transaction.CustomerId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);

    private void ExpectNoTransaction(Guid transactionId, Guid customerId) =>
        _transactions
            .Setup(repository => repository.FindForCustomerAsync(
                transactionId,
                customerId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction?)null);

    private void ExpectNoExistingDisputeFor(Guid transactionId) =>
        _repository
            .Setup(repository => repository.FindForTransactionAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Dispute?)null);

    [Fact]
    public async Task GetAllAsync_passes_the_status_filter_to_the_repository()
    {
        _repository
            .Setup(repository => repository.ListAsync(null, DisputeStatus.Initiated, It.IsAny<CancellationToken>()))
            .ReturnsAsync([DisputeFor(CustomerId)]);

        var result = await _service.GetAllAsync(Admin(), DisputeStatus.Initiated, CancellationToken.None);

        result.Count.ShouldBe(1);
        result[0].Status.ShouldBe("Initiated");
        _repository.Verify(
            repository => repository.ListAsync(null, DisputeStatus.Initiated, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_passes_a_null_filter_through_unchanged()
    {
        _repository
            .Setup(repository => repository.ListAsync(null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _service.GetAllAsync(Admin(), null, CancellationToken.None);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_scopes_a_customer_to_their_own_disputes()
    {
        _repository
            .Setup(repository => repository.ListAsync(CustomerId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await _service.GetAllAsync(Customer(), null, CancellationToken.None);

        _repository.Verify(
            repository => repository.ListAsync(CustomerId, null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_returns_nothing_for_a_customer_whose_token_carries_no_customer_id()
    {
        var result = await _service.GetAllAsync(
            new CallerContext(CustomerEmail, Roles.Customer, null),
            null,
            CancellationToken.None);

        result.ShouldBeEmpty();
        _repository.Verify(
            repository => repository.ListAsync(
                It.IsAny<Guid?>(),
                It.IsAny<DisputeStatus?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_returns_null_when_the_dispute_does_not_exist()
    {
        ExpectFind(MissingId, null);

        var result = await _service.GetByIdAsync(Admin(), MissingId, CancellationToken.None);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetByIdAsync_maps_the_dispute_it_found()
    {
        var dispute = DisputeFor(CustomerId);
        ExpectFind(dispute.Id, dispute);

        var result = await _service.GetByIdAsync(Admin(), dispute.Id, CancellationToken.None);

        result.ShouldNotBeNull();
        result.Id.ShouldBe(dispute.Id);
        result.MerchantName.ShouldBe("Takealot");
        result.Status.ShouldBe("Initiated");
        result.ResolvedAt.ShouldBeNull();
    }

    [Fact]
    public async Task GetByIdAsync_returns_a_customers_own_dispute()
    {
        var dispute = DisputeFor(CustomerId);
        ExpectFind(dispute.Id, dispute);

        var result = await _service.GetByIdAsync(Customer(), dispute.Id, CancellationToken.None);

        result.ShouldNotBeNull();
        result.Id.ShouldBe(dispute.Id);
    }

    [Fact]
    public async Task GetByIdAsync_hides_another_customers_dispute()
    {
        var dispute = DisputeFor(OtherCustomerId);
        ExpectFind(dispute.Id, dispute);

        var result = await _service.GetByIdAsync(Customer(), dispute.Id, CancellationToken.None);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetByIdAsync_lets_staff_see_any_customers_dispute()
    {
        var dispute = DisputeFor(OtherCustomerId);
        ExpectFind(dispute.Id, dispute);

        var result = await _service.GetByIdAsync(Admin(), dispute.Id, CancellationToken.None);

        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetHistoryAsync_returns_the_timeline_oldest_first()
    {
        var dispute = DisputeFor(CustomerId);
        dispute.ChangeStatus(DisputeStatus.UnderInvestigation, AdminEmail, Now);
        ExpectFindWithHistory(dispute.Id, dispute);

        var history = await _service.GetHistoryAsync(Customer(), dispute.Id, CancellationToken.None);

        history.ShouldNotBeNull();
        history.Count.ShouldBe(2);
        history[0].FromStatus.ShouldBeNull();
        history[0].ToStatus.ShouldBe("Initiated");
        history[0].ChangedBy.ShouldBe(CustomerEmail);
        history[1].FromStatus.ShouldBe("Initiated");
        history[1].ToStatus.ShouldBe("UnderInvestigation");
        history[1].ChangedBy.ShouldBe(AdminEmail);
        history[1].ChangedAt.ShouldBe(Now);
    }

    [Fact]
    public async Task GetHistoryAsync_hides_another_customers_dispute()
    {
        var dispute = DisputeFor(OtherCustomerId);
        ExpectFindWithHistory(dispute.Id, dispute);

        var history = await _service.GetHistoryAsync(Customer(), dispute.Id, CancellationToken.None);

        history.ShouldBeNull();
    }

    [Fact]
    public async Task GetHistoryAsync_returns_null_when_the_dispute_does_not_exist()
    {
        ExpectFindWithHistory(MissingId, null);

        var history = await _service.GetHistoryAsync(Admin(), MissingId, CancellationToken.None);

        history.ShouldBeNull();
    }

    [Fact]
    public async Task CreateAsync_derives_the_dispute_from_the_transaction()
    {
        var transaction = TransactionFor(CustomerId, "zar");
        Dispute? added = null;
        ExpectTransaction(transaction);
        ExpectNoExistingDisputeFor(transaction.Id);
        _repository
            .Setup(repository => repository.Add(It.IsAny<Dispute>()))
            .Callback<Dispute>(dispute => added = dispute);
        ExpectSave();

        var response = await _service.CreateAsync(
            Customer(),
            new CreateDisputeRequest(transaction.Id, Reason),
            CancellationToken.None);

        response.ShouldNotBeNull();
        response.Status.ShouldBe("Initiated");
        response.CreatedAt.ShouldBe(Now);
        response.TransactionId.ShouldBe(transaction.Id);
        response.TransactionAmount.ShouldBe(1499.99m);
        response.TransactionCurrency.ShouldBe("ZAR");
        response.TransactionDate.ShouldBe(OccurredAt);
        response.MerchantName.ShouldBe("Takealot");
        response.Reason.ShouldBe(Reason);
        added.ShouldNotBeNull();
        added.Id.ShouldBe(response.Id);
        added.CustomerId.ShouldBe(CustomerId);
        _repository.Verify(
            repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_records_the_raising_customer_as_the_actor()
    {
        var transaction = TransactionFor(CustomerId);
        Dispute? added = null;
        ExpectTransaction(transaction);
        ExpectNoExistingDisputeFor(transaction.Id);
        _repository
            .Setup(repository => repository.Add(It.IsAny<Dispute>()))
            .Callback<Dispute>(dispute => added = dispute);
        ExpectSave();

        await _service.CreateAsync(
            Customer(),
            new CreateDisputeRequest(transaction.Id, Reason),
            CancellationToken.None);

        added.ShouldNotBeNull();
        added.StatusChanges.ShouldHaveSingleItem().ChangedBy.ShouldBe(CustomerEmail);
    }

    [Fact]
    public async Task CreateAsync_returns_null_for_a_transaction_the_caller_does_not_own()
    {
        ExpectNoTransaction(MissingId, CustomerId);

        var result = await _service.CreateAsync(
            Customer(),
            new CreateDisputeRequest(MissingId, Reason),
            CancellationToken.None);

        result.ShouldBeNull();
        _repository.Verify(
            repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_returns_null_for_a_caller_without_a_customer_id()
    {
        var result = await _service.CreateAsync(
            Admin(),
            new CreateDisputeRequest(Guid.CreateVersion7(), Reason),
            CancellationToken.None);

        result.ShouldBeNull();
        _transactions.Verify(
            repository => repository.FindForCustomerAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_rejects_a_second_dispute_on_the_same_transaction()
    {
        var transaction = TransactionFor(CustomerId);
        var existing = DisputeFor(CustomerId);
        ExpectTransaction(transaction);
        _repository
            .Setup(repository => repository.FindForTransactionAsync(
                transaction.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var exception = await Should.ThrowAsync<DisputeAlreadyRaisedException>(
            () => _service.CreateAsync(
                Customer(),
                new CreateDisputeRequest(transaction.Id, Reason),
                CancellationToken.None));

        exception.ExistingDisputeId.ShouldBe(existing.Id);
        _repository.Verify(
            repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ChangeStatusAsync_returns_null_when_the_dispute_does_not_exist()
    {
        ExpectFindWithHistory(MissingId, null);

        var result = await _service.ChangeStatusAsync(
            Admin(), MissingId, DisputeStatus.UnderInvestigation, CancellationToken.None);

        result.ShouldBeNull();
        _repository.Verify(
            repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ChangeStatusAsync_saves_the_advanced_dispute()
    {
        var dispute = DisputeFor(CustomerId);
        ExpectFindWithHistory(dispute.Id, dispute);
        ExpectSave();

        var result = await _service.ChangeStatusAsync(
            Admin(), dispute.Id, DisputeStatus.UnderInvestigation, CancellationToken.None);

        result.ShouldNotBeNull();
        result.Status.ShouldBe("UnderInvestigation");
        _repository.Verify(
            repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ChangeStatusAsync_records_the_acting_staff_member()
    {
        var dispute = DisputeFor(CustomerId);
        ExpectFindWithHistory(dispute.Id, dispute);
        ExpectSave();

        await _service.ChangeStatusAsync(
            Admin(), dispute.Id, DisputeStatus.UnderInvestigation, CancellationToken.None);

        var latest = dispute.StatusChanges.Last();
        latest.ChangedBy.ShouldBe(AdminEmail);
        latest.FromStatus.ShouldBe(DisputeStatus.Initiated);
        latest.ToStatus.ShouldBe(DisputeStatus.UnderInvestigation);
        latest.ChangedAt.ShouldBe(Now);
    }

    [Fact]
    public async Task ChangeStatusAsync_stamps_resolved_at_from_the_clock()
    {
        var dispute = DisputeFor(CustomerId);
        dispute.ChangeStatus(DisputeStatus.UnderInvestigation, AdminEmail, Now);
        ExpectFindWithHistory(dispute.Id, dispute);
        ExpectSave();

        var result = await _service.ChangeStatusAsync(
            Admin(), dispute.Id, DisputeStatus.Resolved, CancellationToken.None);

        result.ShouldNotBeNull();
        result.ResolvedAt.ShouldBe(Now);
    }

    [Fact]
    public async Task ChangeStatusAsync_does_not_save_an_invalid_transition()
    {
        var dispute = DisputeFor(CustomerId);
        ExpectFindWithHistory(dispute.Id, dispute);

        await Should.ThrowAsync<InvalidDisputeStatusTransitionException>(
            () => _service.ChangeStatusAsync(
                Admin(), dispute.Id, DisputeStatus.Resolved, CancellationToken.None));

        _repository.Verify(
            repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ChangeStatusAsync_loads_the_history_it_is_about_to_append_to()
    {
        var dispute = DisputeFor(CustomerId);
        ExpectFindWithHistory(dispute.Id, dispute);
        ExpectSave();

        await _service.ChangeStatusAsync(
            Admin(), dispute.Id, DisputeStatus.UnderInvestigation, CancellationToken.None);

        _repository.Verify(
            repository => repository.FindWithHistoryAsync(dispute.Id, It.IsAny<CancellationToken>()),
            Times.Once);
        _repository.Verify(
            repository => repository.FindAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
