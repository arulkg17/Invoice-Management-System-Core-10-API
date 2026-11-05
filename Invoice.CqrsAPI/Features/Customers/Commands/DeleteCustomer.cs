using Invoice.BAL.Contracts;
using Invoice.CqrsAPI.Cqrs;
using Invoice.Model;

namespace Invoice.CqrsAPI.Features.Customers.Commands;

public sealed record DeleteCustomerCommand(int Id) : ICommand<Unit>;

public sealed class DeleteCustomerValidator : RuleValidator<DeleteCustomerCommand>
{
    public DeleteCustomerValidator()
    {
        Rule(c => c.Id > 0, "Id", "Id must be greater than zero.");
    }
}

public sealed class DeleteCustomerHandler : ICommandHandler<DeleteCustomerCommand, Unit>
{
    private readonly ICustomerService _customers;
    private readonly ISalesInvoiceService _invoices;

    public DeleteCustomerHandler(ICustomerService customers, ISalesInvoiceService invoices)
    {
        _customers = customers;
        _invoices = invoices;
    }

    public async Task<Unit> HandleAsync(DeleteCustomerCommand request, CancellationToken cancellationToken)
    {
        _ = await _customers.GetByIdAsync(request.Id)
            ?? throw new NotFoundException($"Customer with Id {request.Id} was not found.");

        // Business rule: a customer that already has sales invoices cannot be deleted.
        var invoicesCount = (await _invoices.GetAllPagedAsync(null, request.Id, null, 1, 1)).TotalRecords;

        if (invoicesCount > 0)
        {
            throw new BusinessRuleException(
                $"Customer cannot be deleted because it has {invoicesCount} sales invoice(s).");
        }

        var deleted = await _customers.DeleteAsync(request.Id);

        if (!deleted)
        {
            throw new NotFoundException($"Customer with Id {request.Id} was not found.");
        }

        return Unit.Value;
    }
}
