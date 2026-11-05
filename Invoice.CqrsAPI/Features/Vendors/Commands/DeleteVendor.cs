using Invoice.BAL.Contracts;
using Invoice.CqrsAPI.Cqrs;
using Invoice.Model;

namespace Invoice.CqrsAPI.Features.Vendors.Commands;

public sealed record DeleteVendorCommand(int Id) : ICommand<Unit>;

public sealed class DeleteVendorValidator : RuleValidator<DeleteVendorCommand>
{
    public DeleteVendorValidator()
    {
        Rule(c => c.Id > 0, "Id", "Id must be greater than zero.");
    }
}

public sealed class DeleteVendorHandler : ICommandHandler<DeleteVendorCommand, Unit>
{
    private readonly IVendorService _vendors;
    private readonly IPurchaseOrderService _orders;

    public DeleteVendorHandler(IVendorService vendors, IPurchaseOrderService orders)
    {
        _vendors = vendors;
        _orders = orders;
    }

    public async Task<Unit> HandleAsync(DeleteVendorCommand request, CancellationToken cancellationToken)
    {
        _ = await _vendors.GetByIdAsync(request.Id)
            ?? throw new NotFoundException($"Vendor with Id {request.Id} was not found.");

        // Business rule: a vendor that already has purchase orders cannot be deleted.
        var ordersCount = (await _orders.GetAllPagedAsync(null, request.Id, null, 1, 1)).TotalRecords;

        if (ordersCount > 0)
        {
            throw new BusinessRuleException(
                $"Vendor cannot be deleted because it has {ordersCount} purchase order(s).");
        }

        var deleted = await _vendors.DeleteAsync(request.Id);

        if (!deleted)
        {
            throw new NotFoundException($"Vendor with Id {request.Id} was not found.");
        }

        return Unit.Value;
    }
}
