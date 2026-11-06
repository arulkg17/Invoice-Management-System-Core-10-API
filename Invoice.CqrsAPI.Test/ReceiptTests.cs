using Invoice.BAL.Contracts;
using Invoice.CqrsAPI.Features.Receipts;
using Invoice.CqrsAPI.Features.Receipts.Commands;
using Invoice.DTOs;
using Invoice.Model;
using Invoice.CqrsAPI.Messaging;
using Invoice.Events;
using Moq;

namespace Invoice.CqrsAPI.Test;

public class ReceiptTests
{
    private readonly Mock<IReceiptService> _receipts = new();
    private readonly Mock<IEventPublisher> _events = new();

    private static ReceiptData Data(string? number = null, params ReceiptLine[] lines)
        => new(number, new DateTime(2026, 11, 5), 4, null,
               lines.Length == 0 ? new[] { new ReceiptLine(10, 5) } : lines);

    private static ReceiptDto Existing(string status = "Draft")
        => new() { Id = 6, ReceiptNumber = "GRN-000006", PurchaseOrderId = 4, Status = status, CreatedBy = "creator" };

    // ---------------- validation ----------------

    [Fact]
    public void Valid_receipt_has_no_failures()
    {
        Assert.Empty(new CreateReceiptValidator().Validate(new CreateReceiptCommand(Data(), "admin")));
    }

    [Fact]
    public void Zero_quantity_and_missing_po_line_fail()
    {
        var failures = new CreateReceiptValidator().Validate(
            new CreateReceiptCommand(Data(null, new ReceiptLine(0, 0)), "admin"));

        Assert.Contains(failures, f => f.Property == "Lines[0].PurchaseOrderDetailId");
        Assert.Contains(failures, f => f.Property == "Lines[0].ReceivedQuantity");
    }

    [Fact]
    public void Same_po_line_twice_fails()
    {
        var failures = new CreateReceiptValidator().Validate(
            new CreateReceiptCommand(Data(null, new ReceiptLine(10, 1), new ReceiptLine(10, 2)), "admin"));

        Assert.Contains(failures, f => f.Property == "Lines");
    }

    // ---------------- create ----------------

    [Fact]
    public async Task Create_with_blank_number_lets_the_bal_generate_it()
    {
        _receipts.Setup(r => r.AddAsync(It.IsAny<ReceiptDto>())).ReturnsAsync(12);

        var id = await new CreateReceiptHandler(_receipts.Object)
            .HandleAsync(new CreateReceiptCommand(Data("  "), "admin"), CancellationToken.None);

        Assert.Equal(12, id);
        _receipts.Verify(r => r.AddAsync(It.Is<ReceiptDto>(d =>
            d.ReceiptNumber == null && d.Status == "Draft" && d.CreatedBy == "admin" && d.Details.Count == 1)), Times.Once);
    }

    // ---------------- update ----------------

    [Fact]
    public async Task Update_of_a_posted_receipt_is_rejected()
    {
        _receipts.Setup(r => r.GetByIdAsync(6)).ReturnsAsync(Existing("Posted"));

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            new UpdateReceiptHandler(_receipts.Object)
                .HandleAsync(new UpdateReceiptCommand(6, Data(), "editor"), CancellationToken.None));
    }

    [Fact]
    public async Task Update_keeps_the_receipt_number()
    {
        _receipts.Setup(r => r.GetByIdAsync(6)).ReturnsAsync(Existing());
        _receipts.Setup(r => r.UpdateAsync(It.IsAny<ReceiptDto>())).ReturnsAsync(true);

        await new UpdateReceiptHandler(_receipts.Object)
            .HandleAsync(new UpdateReceiptCommand(6, Data(), "editor"), CancellationToken.None);

        _receipts.Verify(r => r.UpdateAsync(It.Is<ReceiptDto>(d =>
            d.ReceiptNumber == "GRN-000006" && d.CreatedBy == "creator" && d.UpdatedBy == "editor")), Times.Once);
    }

    // ---------------- delete ----------------

    [Fact]
    public async Task Delete_of_a_posted_receipt_is_rejected()
    {
        _receipts.Setup(r => r.GetByIdAsync(6)).ReturnsAsync(Existing("Posted"));

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            new DeleteReceiptHandler(_receipts.Object)
                .HandleAsync(new DeleteReceiptCommand(6, "admin"), CancellationToken.None));

        _receipts.Verify(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<string?>()), Times.Never);
    }

    // ---------------- post / cancel ----------------

    [Fact]
    public async Task Post_of_a_non_draft_receipt_is_rejected()
    {
        _receipts.Setup(r => r.GetByIdAsync(6)).ReturnsAsync(Existing("Posted"));

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            new PostReceiptHandler(_receipts.Object, _events.Object)
                .HandleAsync(new PostReceiptCommand(6, "admin"), CancellationToken.None));
    }

    [Fact]
    public async Task Post_of_a_draft_calls_the_service()
    {
        _receipts.Setup(r => r.GetByIdAsync(6)).ReturnsAsync(Existing());
        _receipts.Setup(r => r.PostAsync(6, "admin")).ReturnsAsync(true);

        await new PostReceiptHandler(_receipts.Object, _events.Object)
            .HandleAsync(new PostReceiptCommand(6, "admin"), CancellationToken.None);

        _receipts.Verify(r => r.PostAsync(6, "admin"), Times.Once);
    }

    [Fact]
    public async Task Post_unknown_receipt_throws_not_found()
    {
        _receipts.Setup(r => r.GetByIdAsync(6)).ReturnsAsync((ReceiptDto?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            new PostReceiptHandler(_receipts.Object, _events.Object)
                .HandleAsync(new PostReceiptCommand(6, "admin"), CancellationToken.None));
    }

    [Fact]
    public async Task Cancel_of_an_already_cancelled_receipt_is_rejected()
    {
        _receipts.Setup(r => r.GetByIdAsync(6)).ReturnsAsync(Existing("Cancelled"));

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            new CancelReceiptHandler(_receipts.Object)
                .HandleAsync(new CancelReceiptCommand(6, "admin"), CancellationToken.None));
    }

    // ---------------- events ----------------

    [Fact]
    public async Task Post_publishes_a_ReceiptPosted_event()
    {
        _receipts.Setup(r => r.GetByIdAsync(6)).ReturnsAsync(Existing());
        _receipts.Setup(r => r.PostAsync(6, "admin")).ReturnsAsync(true);

        await new PostReceiptHandler(_receipts.Object, _events.Object)
            .HandleAsync(new PostReceiptCommand(6, "admin"), CancellationToken.None);

        _events.Verify(e => e.PublishAsync(
            It.Is<IntegrationEvent>(x => x is ReceiptPostedEvent
                && ((ReceiptPostedEvent)x).ReceiptId == 6
                && ((ReceiptPostedEvent)x).ReceiptNumber == "GRN-000006"
                && ((ReceiptPostedEvent)x).PurchaseOrderId == 4),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
