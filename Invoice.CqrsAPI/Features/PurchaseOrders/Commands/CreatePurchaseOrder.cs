using Invoice.BAL.Contracts;
using Invoice.CqrsAPI.Common;
using Invoice.CqrsAPI.Cqrs;
using Invoice.DTOs;

namespace Invoice.CqrsAPI.Features.PurchaseOrders.Commands;

public sealed record CreatePurchaseOrderCommand(PurchaseOrderData Data, string CreatedBy)
    : ICommand<int>, IHasPurchaseOrderData;

public sealed class CreatePurchaseOrderValidator : PurchaseOrderDataValidator<CreatePurchaseOrderCommand>
{
}

public sealed class CreatePurchaseOrderHandler : ICommandHandler<CreatePurchaseOrderCommand, int>
{
    private readonly IPurchaseOrderService _orders;
    private readonly IReferenceChecks _references;

    public CreatePurchaseOrderHandler(IPurchaseOrderService orders, IReferenceChecks references)
    {
        _orders = orders;
        _references = references;
    }

    public async Task<int> HandleAsync(CreatePurchaseOrderCommand request, CancellationToken cancellationToken)
    {
        var data = request.Data;

        // Handler: "do the referenced records exist?". BAL: totals, line maths, status.
        await _references.EnsureVendorAsync(data.VendorId);
        await _references.EnsureItemsAsync(data.Lines.Select(l => l.ItemmasterId));

        var dto = new PurchaseOrderDto
        {
            PONumber = data.PONumber?.Trim() ?? string.Empty,
            PODate = data.PODate,
            VendorId = data.VendorId,
            Status = DocumentStatus.Draft,
            Notes = data.Notes,
            CreatedBy = request.CreatedBy,
            Details = data.ToDetails()
        };

        return await _orders.AddAsync(dto);
    }
}
