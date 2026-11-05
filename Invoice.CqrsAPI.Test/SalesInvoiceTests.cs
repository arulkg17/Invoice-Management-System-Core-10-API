using Invoice.BAL.Contracts;
using Invoice.CqrsAPI.Common;
using Invoice.CqrsAPI.Features.SalesInvoices;
using Invoice.CqrsAPI.Features.SalesInvoices.Commands;
using Invoice.DTOs;
using Invoice.Model;
using Moq;

namespace Invoice.CqrsAPI.Test;

public class SalesInvoiceTests
{
    private readonly Mock<ISalesInvoiceService> _invoices = new();
    private readonly Mock<IReferenceChecks> _references = new();

    private static SalesInvoiceData Data(DateTime? due = null, string? number = null)
        => new(number, new DateTime(2026, 11, 5), due, 2, null,
               new[] { new DocumentLine(7, 2, 100, 0, 18) });

    private static SalesInvoiceDto Existing(string status = "Draft")
        => new() { Id = 8, InvoiceNumber = "INV-000008", CustomerId = 2, Status = status, CreatedBy = "creator" };

    // ---------------- validation ----------------

    [Fact]
    public void Valid_invoice_has_no_failures()
    {
        Assert.Empty(new CreateSalesInvoiceValidator()
            .Validate(new CreateSalesInvoiceCommand(Data(new DateTime(2026, 12, 5)), "admin")));
    }

    [Fact]
    public void Due_date_before_invoice_date_fails()
    {
        var failures = new CreateSalesInvoiceValidator()
            .Validate(new CreateSalesInvoiceCommand(Data(new DateTime(2026, 11, 1)), "admin"));

        Assert.Contains(failures, f => f.Property == "DueDate");
    }

    [Fact]
    public void Missing_customer_fails()
    {
        var data = Data() with { CustomerId = 0 };

        var failures = new CreateSalesInvoiceValidator().Validate(new CreateSalesInvoiceCommand(data, "admin"));

        Assert.Contains(failures, f => f.Property == "CustomerId");
    }

    // ---------------- create ----------------

    [Fact]
    public async Task Create_checks_customer_and_items_then_saves_a_draft()
    {
        _invoices.Setup(i => i.AddAsync(It.IsAny<SalesInvoiceDto>())).ReturnsAsync(44);

        var id = await new CreateSalesInvoiceHandler(_invoices.Object, _references.Object)
            .HandleAsync(new CreateSalesInvoiceCommand(Data(number: " "), "admin"), CancellationToken.None);

        Assert.Equal(44, id);
        _references.Verify(r => r.EnsureCustomerAsync(2), Times.Once);
        _references.Verify(r => r.EnsureItemsAsync(It.Is<IEnumerable<int>>(ids => ids.Contains(7))), Times.Once);
        _invoices.Verify(i => i.AddAsync(It.Is<SalesInvoiceDto>(d =>
            d.Status == "Draft" && d.InvoiceNumber == null && d.CreatedBy == "admin")), Times.Once);
    }

    [Fact]
    public async Task Create_for_an_inactive_customer_never_reaches_the_service()
    {
        _references.Setup(r => r.EnsureCustomerAsync(2))
            .ThrowsAsync(new BusinessRuleException("Customer 'Acme' is inactive."));

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            new CreateSalesInvoiceHandler(_invoices.Object, _references.Object)
                .HandleAsync(new CreateSalesInvoiceCommand(Data(), "admin"), CancellationToken.None));

        _invoices.Verify(i => i.AddAsync(It.IsAny<SalesInvoiceDto>()), Times.Never);
    }

    // ---------------- update / delete ----------------

    [Fact]
    public async Task Update_of_a_posted_invoice_is_rejected()
    {
        _invoices.Setup(i => i.GetByIdAsync(8)).ReturnsAsync(Existing("Posted"));

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            new UpdateSalesInvoiceHandler(_invoices.Object, _references.Object)
                .HandleAsync(new UpdateSalesInvoiceCommand(8, Data(), "editor"), CancellationToken.None));
    }

    [Fact]
    public async Task Update_keeps_the_invoice_number_and_created_audit_values()
    {
        _invoices.Setup(i => i.GetByIdAsync(8)).ReturnsAsync(Existing());
        _invoices.Setup(i => i.UpdateAsync(It.IsAny<SalesInvoiceDto>())).ReturnsAsync(true);

        await new UpdateSalesInvoiceHandler(_invoices.Object, _references.Object)
            .HandleAsync(new UpdateSalesInvoiceCommand(8, Data(), "editor"), CancellationToken.None);

        _invoices.Verify(i => i.UpdateAsync(It.Is<SalesInvoiceDto>(d =>
            d.InvoiceNumber == "INV-000008" && d.CreatedBy == "creator" && d.UpdatedBy == "editor")), Times.Once);
    }

    [Fact]
    public async Task Delete_of_a_posted_invoice_is_rejected()
    {
        _invoices.Setup(i => i.GetByIdAsync(8)).ReturnsAsync(Existing("Posted"));

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            new DeleteSalesInvoiceHandler(_invoices.Object)
                .HandleAsync(new DeleteSalesInvoiceCommand(8, "admin"), CancellationToken.None));

        _invoices.Verify(i => i.DeleteAsync(It.IsAny<int>(), It.IsAny<string?>()), Times.Never);
    }

    // ---------------- post / cancel ----------------

    [Fact]
    public async Task Post_of_a_draft_calls_the_service()
    {
        _invoices.Setup(i => i.GetByIdAsync(8)).ReturnsAsync(Existing());
        _invoices.Setup(i => i.PostAsync(8, "admin")).ReturnsAsync(true);

        await new PostSalesInvoiceHandler(_invoices.Object)
            .HandleAsync(new PostSalesInvoiceCommand(8, "admin"), CancellationToken.None);

        _invoices.Verify(i => i.PostAsync(8, "admin"), Times.Once);
    }

    [Fact]
    public async Task Post_of_a_cancelled_invoice_is_rejected()
    {
        _invoices.Setup(i => i.GetByIdAsync(8)).ReturnsAsync(Existing("Cancelled"));

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            new PostSalesInvoiceHandler(_invoices.Object)
                .HandleAsync(new PostSalesInvoiceCommand(8, "admin"), CancellationToken.None));
    }

    [Fact]
    public async Task Cancel_of_an_already_cancelled_invoice_is_rejected()
    {
        _invoices.Setup(i => i.GetByIdAsync(8)).ReturnsAsync(Existing("Cancelled"));

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            new CancelSalesInvoiceHandler(_invoices.Object)
                .HandleAsync(new CancelSalesInvoiceCommand(8, "admin"), CancellationToken.None));
    }
}
