using Invoice.BAL.Contracts;
using Invoice.CqrsAPI.Common;
using Invoice.CqrsAPI.Cqrs;
using Invoice.DTOs;
using Invoice.Model;

namespace Invoice.CqrsAPI.Features.SalesInvoices.Commands;

public sealed record UpdateSalesInvoiceCommand(int Id, SalesInvoiceData Data, string UpdatedBy)
    : ICommand<Unit>, IHasSalesInvoiceData;

public sealed class UpdateSalesInvoiceValidator : SalesInvoiceDataValidator<UpdateSalesInvoiceCommand>
{
    public UpdateSalesInvoiceValidator()
    {
        Rule(c => c.Id > 0, "Id", "Id must be greater than zero.");
    }
}

public sealed class UpdateSalesInvoiceHandler : ICommandHandler<UpdateSalesInvoiceCommand, Unit>
{
    private readonly ISalesInvoiceService _invoices;
    private readonly IReferenceChecks _references;

    public UpdateSalesInvoiceHandler(ISalesInvoiceService invoices, IReferenceChecks references)
    {
        _invoices = invoices;
        _references = references;
    }

    public async Task<Unit> HandleAsync(UpdateSalesInvoiceCommand request, CancellationToken cancellationToken)
    {
        var existing = await _invoices.GetByIdAsync(request.Id)
            ?? throw new NotFoundException($"Sales invoice with Id {request.Id} was not found.");

        if (existing.Status != DocumentStatus.Draft)
        {
            throw new BusinessRuleException("Only Draft sales invoices can be edited.");
        }

        var data = request.Data;

        await _references.EnsureCustomerAsync(data.CustomerId);
        await _references.EnsureItemsAsync(data.Lines.Select(l => l.ItemmasterId));

        var dto = new SalesInvoiceDto
        {
            Id = request.Id,
            InvoiceNumber = string.IsNullOrWhiteSpace(data.InvoiceNumber) ? existing.InvoiceNumber : data.InvoiceNumber.Trim(),
            InvoiceDate = data.InvoiceDate,
            DueDate = data.DueDate,
            CustomerId = data.CustomerId,
            Status = existing.Status,
            Notes = data.Notes,
            CreatedBy = existing.CreatedBy,
            CreatedDate = existing.CreatedDate,
            UpdatedBy = request.UpdatedBy,
            Details = data.ToDetails()
        };

        var updated = await _invoices.UpdateAsync(dto);

        if (!updated)
        {
            throw new NotFoundException($"Sales invoice with Id {request.Id} was not found.");
        }

        return Unit.Value;
    }
}
