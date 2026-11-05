using Invoice.BAL.Contracts;
using Invoice.CqrsAPI.Common;
using Invoice.CqrsAPI.Cqrs;
using Invoice.Model;

namespace Invoice.CqrsAPI.Features.SalesInvoices.Commands;

public sealed record DeleteSalesInvoiceCommand(int Id, string DeletedBy) : ICommand<Unit>;

public sealed class DeleteSalesInvoiceValidator : RuleValidator<DeleteSalesInvoiceCommand>
{
    public DeleteSalesInvoiceValidator()
    {
        Rule(c => c.Id > 0, "Id", "Id must be greater than zero.");
    }
}

public sealed class DeleteSalesInvoiceHandler : ICommandHandler<DeleteSalesInvoiceCommand, Unit>
{
    private readonly ISalesInvoiceService _invoices;

    public DeleteSalesInvoiceHandler(ISalesInvoiceService invoices)
    {
        _invoices = invoices;
    }

    public async Task<Unit> HandleAsync(DeleteSalesInvoiceCommand request, CancellationToken cancellationToken)
    {
        var existing = await _invoices.GetByIdAsync(request.Id)
            ?? throw new NotFoundException($"Sales invoice with Id {request.Id} was not found.");

        // Business rule: a Posted invoice has already reduced stock. Cancel it instead of deleting it.
        if (existing.Status is not (DocumentStatus.Draft or DocumentStatus.Cancelled))
        {
            throw new BusinessRuleException(
                $"A {existing.Status} sales invoice cannot be deleted. Only Draft or Cancelled ones can.");
        }

        var deleted = await _invoices.DeleteAsync(request.Id, request.DeletedBy);

        if (!deleted)
        {
            throw new NotFoundException($"Sales invoice with Id {request.Id} was not found.");
        }

        return Unit.Value;
    }
}
