using Invoice.BAL.Contracts;
using Invoice.CqrsAPI.Common;
using Invoice.CqrsAPI.Cqrs;
using Invoice.Model;

namespace Invoice.CqrsAPI.Features.PurchaseOrders.Commands;

public sealed record CancelPurchaseOrderCommand(int Id, string CancelledBy) : ICommand<Unit>;

public sealed class CancelPurchaseOrderValidator : RuleValidator<CancelPurchaseOrderCommand>
{
    public CancelPurchaseOrderValidator()
    {
        Rule(c => c.Id > 0, "Id", "Id must be greater than zero.");
    }
}

public sealed class CancelPurchaseOrderHandler : ICommandHandler<CancelPurchaseOrderCommand, Unit>
{
    private readonly IPurchaseOrderService _orders;

    public CancelPurchaseOrderHandler(IPurchaseOrderService orders)
    {
        _orders = orders;
    }

    public async Task<Unit> HandleAsync(CancelPurchaseOrderCommand request, CancellationToken cancellationToken)
    {
        var existing = await _orders.GetByIdAsync(request.Id)
            ?? throw new NotFoundException($"Purchase order with Id {request.Id} was not found.");

        if (existing.Status == DocumentStatus.Cancelled)
        {
            throw new BusinessRuleException("This purchase order is already cancelled.");
        }

        if (!await _orders.CancelAsync(request.Id, request.CancelledBy))
        {
            throw new BusinessRuleException("The purchase order could not be cancelled.");
        }

        return Unit.Value;
    }
}
