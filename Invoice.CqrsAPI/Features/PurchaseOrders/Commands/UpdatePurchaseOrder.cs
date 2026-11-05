using Invoice.BAL.Contracts;
using Invoice.CqrsAPI.Common;
using Invoice.CqrsAPI.Cqrs;
using Invoice.DTOs;
using Invoice.Model;

namespace Invoice.CqrsAPI.Features.PurchaseOrders.Commands;

public sealed record UpdatePurchaseOrderCommand(int Id, PurchaseOrderData Data, string UpdatedBy)
    : ICommand<Unit>, IHasPurchaseOrderData;

public sealed class UpdatePurchaseOrderValidator : PurchaseOrderDataValidator<UpdatePurchaseOrderCommand>
{
    public UpdatePurchaseOrderValidator()
    {
        Rule(c => c.Id > 0, "Id", "Id must be greater than zero.");
    }
}

public sealed class UpdatePurchaseOrderHandler : ICommandHandler<UpdatePurchaseOrderCommand, Unit>
{
    private readonly IPurchaseOrderService _orders;
    private readonly IReferenceChecks _references;

    public UpdatePurchaseOrderHandler(IPurchaseOrderService orders, IReferenceChecks references)
    {
        _orders = orders;
        _references = references;
    }

    public async Task<Unit> HandleAsync(UpdatePurchaseOrderCommand request, CancellationToken cancellationToken)
    {
        var existing = await _orders.GetByIdAsync(request.Id)
            ?? throw new NotFoundException($"Purchase order with Id {request.Id} was not found.");

        if (existing.Status != DocumentStatus.Draft)
        {
            throw new BusinessRuleException("Only Draft purchase orders can be edited.");
        }

        var data = request.Data;

        await _references.EnsureVendorAsync(data.VendorId);
        await _references.EnsureItemsAsync(data.Lines.Select(l => l.ItemmasterId));

        var dto = new PurchaseOrderDto
        {
            Id = request.Id,
            PONumber = string.IsNullOrWhiteSpace(data.PONumber) ? existing.PONumber : data.PONumber.Trim(),
            PODate = data.PODate,
            VendorId = data.VendorId,
            Status = existing.Status,
            Notes = data.Notes,
            CreatedBy = existing.CreatedBy,
            CreatedDate = existing.CreatedDate,
            UpdatedBy = request.UpdatedBy,
            Details = data.ToDetails()
        };

        var updated = await _orders.UpdateAsync(dto);

        if (!updated)
        {
            throw new NotFoundException($"Purchase order with Id {request.Id} was not found.");
        }

        return Unit.Value;
    }
}
