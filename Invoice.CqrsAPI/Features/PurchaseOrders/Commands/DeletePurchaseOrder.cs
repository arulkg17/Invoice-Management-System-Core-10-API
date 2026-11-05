using Invoice.BAL.Contracts;
using Invoice.CqrsAPI.Common;
using Invoice.CqrsAPI.Cqrs;
using Invoice.Model;

namespace Invoice.CqrsAPI.Features.PurchaseOrders.Commands;

public sealed record DeletePurchaseOrderCommand(int Id) : ICommand<Unit>;

public sealed class DeletePurchaseOrderValidator : RuleValidator<DeletePurchaseOrderCommand>
{
    public DeletePurchaseOrderValidator()
    {
        Rule(c => c.Id > 0, "Id", "Id must be greater than zero.");
    }
}

public sealed class DeletePurchaseOrderHandler : ICommandHandler<DeletePurchaseOrderCommand, Unit>
{
    private readonly IPurchaseOrderService _orders;

    public DeletePurchaseOrderHandler(IPurchaseOrderService orders)
    {
        _orders = orders;
    }

    public async Task<Unit> HandleAsync(DeletePurchaseOrderCommand request, CancellationToken cancellationToken)
    {
        var existing = await _orders.GetByIdAsync(request.Id)
            ?? throw new NotFoundException($"Purchase order with Id {request.Id} was not found.");

        // Business rule: an approved / received order is part of the paper trail. Cancel it instead.
        if (existing.Status is not (DocumentStatus.Draft or DocumentStatus.Cancelled))
        {
            throw new BusinessRuleException(
                $"A {existing.Status} purchase order cannot be deleted. Only Draft or Cancelled ones can.");
        }

        var deleted = await _orders.DeleteAsync(request.Id);

        if (!deleted)
        {
            throw new NotFoundException($"Purchase order with Id {request.Id} was not found.");
        }

        return Unit.Value;
    }
}
