using Invoice.BAL.Contracts;
using Invoice.CqrsAPI.Common;
using Invoice.CqrsAPI.Features.PurchaseOrders;
using Invoice.CqrsAPI.Features.PurchaseOrders.Commands;
using Invoice.DTOs;
using Invoice.Model;
using Moq;

namespace Invoice.CqrsAPI.Test;

public class PurchaseOrderTests
{
    private readonly Mock<IPurchaseOrderService> _orders = new();
    private readonly Mock<IReferenceChecks> _references = new();

    private static PurchaseOrderData Data(params DocumentLine[] lines)
        => new(null, new DateTime(2026, 11, 5), 3, "note",
               lines.Length == 0 ? new[] { new DocumentLine(7, 2, 100, 0, 18) } : lines);

    private static PurchaseOrderDto Existing(string status = "Draft")
        => new() { Id = 5, PONumber = "PO-1", VendorId = 3, Status = status, CreatedBy = "creator" };

    // ---------------- validation ----------------

    [Fact]
    public void Valid_order_has_no_failures()
    {
        Assert.Empty(new CreatePurchaseOrderValidator().Validate(new CreatePurchaseOrderCommand(Data(), "admin")));
    }

    [Fact]
    public void Order_without_lines_fails()
    {
        var data = new PurchaseOrderData(null, new DateTime(2026, 11, 5), 3, null, Array.Empty<DocumentLine>());

        var failures = new CreatePurchaseOrderValidator().Validate(new CreatePurchaseOrderCommand(data, "admin"));

        Assert.Contains(failures, f => f.Property == "Lines");
    }

    [Fact]
    public void Bad_line_reports_the_line_and_the_field()
    {
        var failures = new CreatePurchaseOrderValidator().Validate(new CreatePurchaseOrderCommand(
            Data(new DocumentLine(7, 0, 100, 0, 18), new DocumentLine(8, 1, 100, 500, 18)), "admin"));

        Assert.Contains(failures, f => f.Property == "Lines[0].Quantity");
        Assert.Contains(failures, f => f.Property == "Lines[1].DiscountAmount");
    }

    // ---------------- create ----------------

    [Fact]
    public async Task Create_checks_references_then_saves_a_draft()
    {
        _orders.Setup(o => o.AddAsync(It.IsAny<PurchaseOrderDto>())).ReturnsAsync(31);

        var id = await new CreatePurchaseOrderHandler(_orders.Object, _references.Object)
            .HandleAsync(new CreatePurchaseOrderCommand(Data(), "admin"), CancellationToken.None);

        Assert.Equal(31, id);
        _references.Verify(r => r.EnsureVendorAsync(3), Times.Once);
        _references.Verify(r => r.EnsureItemsAsync(It.Is<IEnumerable<int>>(ids => ids.Contains(7))), Times.Once);
        _orders.Verify(o => o.AddAsync(It.Is<PurchaseOrderDto>(d =>
            d.Status == "Draft" && d.CreatedBy == "admin" && d.Details.Count == 1 && d.VendorId == 3)), Times.Once);
    }

    [Fact]
    public async Task Create_with_unknown_vendor_never_reaches_the_service()
    {
        _references.Setup(r => r.EnsureVendorAsync(3)).ThrowsAsync(new NotFoundException("Vendor with Id 3 was not found."));

        await Assert.ThrowsAsync<NotFoundException>(() =>
            new CreatePurchaseOrderHandler(_orders.Object, _references.Object)
                .HandleAsync(new CreatePurchaseOrderCommand(Data(), "admin"), CancellationToken.None));

        _orders.Verify(o => o.AddAsync(It.IsAny<PurchaseOrderDto>()), Times.Never);
    }

    // ---------------- update ----------------

    [Fact]
    public async Task Update_of_an_approved_order_is_rejected()
    {
        _orders.Setup(o => o.GetByIdAsync(5)).ReturnsAsync(Existing("Approved"));

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            new UpdatePurchaseOrderHandler(_orders.Object, _references.Object)
                .HandleAsync(new UpdatePurchaseOrderCommand(5, Data(), "editor"), CancellationToken.None));

        _orders.Verify(o => o.UpdateAsync(It.IsAny<PurchaseOrderDto>()), Times.Never);
    }

    [Fact]
    public async Task Update_keeps_po_number_and_created_audit_values()
    {
        _orders.Setup(o => o.GetByIdAsync(5)).ReturnsAsync(Existing());
        _orders.Setup(o => o.UpdateAsync(It.IsAny<PurchaseOrderDto>())).ReturnsAsync(true);

        await new UpdatePurchaseOrderHandler(_orders.Object, _references.Object)
            .HandleAsync(new UpdatePurchaseOrderCommand(5, Data(), "editor"), CancellationToken.None);

        _orders.Verify(o => o.UpdateAsync(It.Is<PurchaseOrderDto>(d =>
            d.Id == 5 && d.PONumber == "PO-1" && d.CreatedBy == "creator" && d.UpdatedBy == "editor")), Times.Once);
    }

    // ---------------- delete ----------------

    [Fact]
    public async Task Delete_of_an_approved_order_is_rejected()
    {
        _orders.Setup(o => o.GetByIdAsync(5)).ReturnsAsync(Existing("Approved"));

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            new DeletePurchaseOrderHandler(_orders.Object)
                .HandleAsync(new DeletePurchaseOrderCommand(5), CancellationToken.None));

        _orders.Verify(o => o.DeleteAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Delete_of_a_draft_calls_the_service()
    {
        _orders.Setup(o => o.GetByIdAsync(5)).ReturnsAsync(Existing());
        _orders.Setup(o => o.DeleteAsync(5)).ReturnsAsync(true);

        await new DeletePurchaseOrderHandler(_orders.Object)
            .HandleAsync(new DeletePurchaseOrderCommand(5), CancellationToken.None);

        _orders.Verify(o => o.DeleteAsync(5), Times.Once);
    }

    // ---------------- approve / cancel ----------------

    [Fact]
    public async Task Approve_of_a_non_draft_order_is_rejected()
    {
        _orders.Setup(o => o.GetByIdAsync(5)).ReturnsAsync(Existing("Approved"));

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            new ApprovePurchaseOrderHandler(_orders.Object)
                .HandleAsync(new ApprovePurchaseOrderCommand(5, "boss"), CancellationToken.None));

        _orders.Verify(o => o.ApproveAsync(It.IsAny<int>(), It.IsAny<string?>()), Times.Never);
    }

    [Fact]
    public async Task Approve_of_a_draft_calls_the_service_with_the_user()
    {
        _orders.Setup(o => o.GetByIdAsync(5)).ReturnsAsync(Existing());
        _orders.Setup(o => o.ApproveAsync(5, "boss")).ReturnsAsync(true);

        await new ApprovePurchaseOrderHandler(_orders.Object)
            .HandleAsync(new ApprovePurchaseOrderCommand(5, "boss"), CancellationToken.None);

        _orders.Verify(o => o.ApproveAsync(5, "boss"), Times.Once);
    }

    [Fact]
    public async Task Approve_unknown_order_throws_not_found()
    {
        _orders.Setup(o => o.GetByIdAsync(5)).ReturnsAsync((PurchaseOrderDto?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            new ApprovePurchaseOrderHandler(_orders.Object)
                .HandleAsync(new ApprovePurchaseOrderCommand(5, "boss"), CancellationToken.None));
    }

    [Fact]
    public async Task Cancel_of_an_already_cancelled_order_is_rejected()
    {
        _orders.Setup(o => o.GetByIdAsync(5)).ReturnsAsync(Existing("Cancelled"));

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            new CancelPurchaseOrderHandler(_orders.Object)
                .HandleAsync(new CancelPurchaseOrderCommand(5, "boss"), CancellationToken.None));
    }
}
