using Invoice.BAL.Contracts;
using Invoice.CqrsAPI.Common;
using Invoice.CqrsAPI.Cqrs;
using Invoice.DTOs;

namespace Invoice.CqrsAPI.Features.SalesInvoices.Commands;

public sealed record CreateSalesInvoiceCommand(SalesInvoiceData Data, string CreatedBy)
    : ICommand<int>, IHasSalesInvoiceData;

public sealed class CreateSalesInvoiceValidator : SalesInvoiceDataValidator<CreateSalesInvoiceCommand>
{
}

public sealed class CreateSalesInvoiceHandler : ICommandHandler<CreateSalesInvoiceCommand, int>
{
    private readonly ISalesInvoiceService _invoices;
    private readonly IReferenceChecks _references;

    public CreateSalesInvoiceHandler(ISalesInvoiceService invoices, IReferenceChecks references)
    {
        _invoices = invoices;
        _references = references;
    }

    public async Task<int> HandleAsync(CreateSalesInvoiceCommand request, CancellationToken cancellationToken)
    {
        var data = request.Data;

        await _references.EnsureCustomerAsync(data.CustomerId);
        await _references.EnsureItemsAsync(data.Lines.Select(l => l.ItemmasterId));

        var dto = new SalesInvoiceDto
        {
            // Empty number = let the BAL generate INV-000001 ...
            InvoiceNumber = string.IsNullOrWhiteSpace(data.InvoiceNumber) ? null : data.InvoiceNumber.Trim(),
            InvoiceDate = data.InvoiceDate,
            DueDate = data.DueDate,
            CustomerId = data.CustomerId,
            Status = DocumentStatus.Draft,
            Notes = data.Notes,
            CreatedBy = request.CreatedBy,
            Details = data.ToDetails()
        };

        return await _invoices.AddAsync(dto);
    }
}
