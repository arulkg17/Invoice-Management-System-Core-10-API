using Invoice.BAL.Contracts;
using Invoice.CqrsAPI.Common;
using Invoice.CqrsAPI.Cqrs;
using Invoice.Model;

namespace Invoice.CqrsAPI.Features.PurchaseOrders.Commands;

/// <summary>Draft -> Approved. A workflow step is a command with a name, not a generic "update".</summary>
public sealed record ApprovePurchaseOrderCommand(int Id, string ApprovedBy) : ICommand<Unit>;

public sealed class ApprovePurchaseOrderValidator : RuleValidator<ApprovePurchaseOrderCommand>
{
    public ApprovePurchaseOrderValidator()
    {
        Rule(c => c.Id > 0, "Id", "Id must be greater than zero.");
    }
}

public sealed class ApprovePurchaseOrderHandler : ICommandHandler<ApprovePurchaseOrderCommand, Unit>
{
    private readonly IPurchaseOrderService _orders;

    public ApprovePurchaseOrderHandler(IPurchaseOrderService orders)
    {
        _orders = orders;
    }

    public async Task<Unit> HandleAsync(ApprovePurchaseOrderCommand request, CancellationToken cancellationToken)
    {
        var existing = await _orders.GetByIdAsync(request.Id)
            ?? throw new NotFoundException($"Purchase order with Id {request.Id} was not found.");

        if (existing.Status != DocumentStatus.Draft)
        {
            throw new BusinessRuleException(
                $"Only Draft purchase orders can be approved. This one is {existing.Status}.");
        }

        if (!await _orders.ApproveAsync(request.Id, request.ApprovedBy))
        {
            throw new BusinessRuleException("The purchase order could not be approved.");
        }

        return Unit.Value;
    }
}
