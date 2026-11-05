using Invoice.BAL.Contracts;
using Invoice.CqrsAPI.Common;
using Invoice.CqrsAPI.Cqrs;
using Invoice.Model;

namespace Invoice.CqrsAPI.Features.SalesInvoices.Commands;

public sealed record CancelSalesInvoiceCommand(int Id, string CancelledBy) : ICommand<Unit>;

public sealed class CancelSalesInvoiceValidator : RuleValidator<CancelSalesInvoiceCommand>
{
    public CancelSalesInvoiceValidator()
    {
        Rule(c => c.Id > 0, "Id", "Id must be greater than zero.");
    }
}

public sealed class CancelSalesInvoiceHandler : ICommandHandler<CancelSalesInvoiceCommand, Unit>
{
    private readonly ISalesInvoiceService _invoices;

    public CancelSalesInvoiceHandler(ISalesInvoiceService invoices)
    {
        _invoices = invoices;
    }

    public async Task<Unit> HandleAsync(CancelSalesInvoiceCommand request, CancellationToken cancellationToken)
    {
        var existing = await _invoices.GetByIdAsync(request.Id)
            ?? throw new NotFoundException($"Sales invoice with Id {request.Id} was not found.");

        if (existing.Status == DocumentStatus.Cancelled)
        {
            throw new BusinessRuleException("This sales invoice is already cancelled.");
        }

        if (!await _invoices.CancelAsync(request.Id, request.CancelledBy))
        {
            throw new BusinessRuleException("The sales invoice could not be cancelled.");
        }

        return Unit.Value;
    }
}
