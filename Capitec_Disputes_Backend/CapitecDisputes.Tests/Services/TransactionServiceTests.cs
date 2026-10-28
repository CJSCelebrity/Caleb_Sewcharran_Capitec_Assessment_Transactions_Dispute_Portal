using System;
using System.Threading;
using System.Threading.Tasks;
using CapitecDisputes.Application.Abstractions;
using CapitecDisputes.Application.Authorization;
using CapitecDisputes.Application.Services;
using CapitecDisputes.Domain.Entities;
using Moq;
using Shouldly;
using Xunit;

namespace CapitecDisputes.Tests.Services;

public class TransactionServiceTests
{
    private static readonly DateTimeOffset OccurredAt = new(2026, 10, 2, 9, 14, 0, TimeSpan.Zero);
    private static readonly Guid CustomerId = Guid.Parse("0199c0de-0003-7000-8000-000000000001");

    private const string CustomerEmail = "thandi@example.test";

    private readonly Mock<ITransactionRepository> _transactions = new(MockBehavior.Strict);
    private readonly Mock<IDisputeRepository> _disputes = new(MockBehavior.Strict);
    private readonly TransactionService _service;

    public TransactionServiceTests() =>
        _service = new TransactionService(_transactions.Object, _disputes.Object);

    private static CallerContext Customer() => new(CustomerEmail, Roles.Customer, CustomerId);

    private static Transaction TransactionFor(string reference) =>
        Transaction.Record(reference, CustomerId, 1499.99m, "ZAR", OccurredAt, "Takealot");

    private void Arrange(Transaction[] transactions, Dispute[] disputes)
    {
        _transactions
            .Setup(repository => repository.ListForCustomerAsync(CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactions);
        _disputes
            .Setup(repository => repository.ListAsync(CustomerId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(disputes);
    }

    [Fact]
    public async Task GetForCustomerAsync_lists_the_callers_transactions()
    {
        var transaction = TransactionFor("TXN-1001");
        Arrange([transaction], []);

        var result = await _service.GetForCustomerAsync(Customer(), CancellationToken.None);

        var only = result.ShouldHaveSingleItem();
        only.Id.ShouldBe(transaction.Id);
        only.Reference.ShouldBe("TXN-1001");
        only.Amount.ShouldBe(1499.99m);
        only.Currency.ShouldBe("ZAR");
        only.OccurredAt.ShouldBe(OccurredAt);
        only.MerchantName.ShouldBe("Takealot");
    }

    [Fact]
    public async Task GetForCustomerAsync_marks_a_transaction_that_has_a_dispute()
    {
        var transaction = TransactionFor("TXN-1001");
        var dispute = Dispute.Raise(transaction, "Charged twice.", CustomerEmail, OccurredAt);
        Arrange([transaction], [dispute]);

        var only = (await _service.GetForCustomerAsync(Customer(), CancellationToken.None)).ShouldHaveSingleItem();

        only.DisputeId.ShouldBe(dispute.Id);
        only.DisputeStatus.ShouldBe("Initiated");
    }

    [Fact]
    public async Task GetForCustomerAsync_reports_the_current_status_of_an_advanced_dispute()
    {
        var transaction = TransactionFor("TXN-1001");
        var dispute = Dispute.Raise(transaction, "Charged twice.", CustomerEmail, OccurredAt);
        dispute.ChangeStatus(DisputeStatus.UnderInvestigation, "admin@capitec.test", OccurredAt);
        Arrange([transaction], [dispute]);

        var only = (await _service.GetForCustomerAsync(Customer(), CancellationToken.None)).ShouldHaveSingleItem();

        only.DisputeStatus.ShouldBe("UnderInvestigation");
    }

    [Fact]
    public async Task GetForCustomerAsync_leaves_an_undisputed_transaction_unmarked()
    {
        Arrange([TransactionFor("TXN-1001")], []);

        var only = (await _service.GetForCustomerAsync(Customer(), CancellationToken.None)).ShouldHaveSingleItem();

        only.DisputeId.ShouldBeNull();
        only.DisputeStatus.ShouldBeNull();
    }

    [Fact]
    public async Task GetForCustomerAsync_only_marks_the_transaction_the_dispute_belongs_to()
    {
        var disputed = TransactionFor("TXN-1001");
        var untouched = TransactionFor("TXN-1002");
        var dispute = Dispute.Raise(disputed, "Charged twice.", CustomerEmail, OccurredAt);
        Arrange([disputed, untouched], [dispute]);

        var result = await _service.GetForCustomerAsync(Customer(), CancellationToken.None);

        result.Count.ShouldBe(2);
        result[0].DisputeId.ShouldBe(dispute.Id);
        result[1].DisputeId.ShouldBeNull();
    }

    [Fact]
    public async Task GetForCustomerAsync_returns_nothing_for_a_caller_without_a_customer_id()
    {
        var result = await _service.GetForCustomerAsync(
            new CallerContext("admin@capitec.test", Roles.DisputeAdmin, null),
            CancellationToken.None);

        result.ShouldBeEmpty();
        _transactions.Verify(
            repository => repository.ListForCustomerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
