using Invoice.CqrsAPI.Common;
using Invoice.CqrsAPI.Cqrs;
using Invoice.DTOs;

namespace Invoice.CqrsAPI.Features.PurchaseOrders;

/// <summary>The editable part of a purchase order. Totals, status and audit columns are NOT here: the BAL owns them.</summary>
public sealed record PurchaseOrderData(
    string? PONumber,
    DateTime PODate,
    int VendorId,
    string? Notes,
    IReadOnlyList<DocumentLine> Lines)
{
    public static PurchaseOrderData From(PurchaseOrderDto dto) => new(
        dto.PONumber,
        dto.PODate,
        dto.VendorId,
        dto.Notes,
        dto.Details?
            .Select(d => new DocumentLine(d.ItemmasterId, d.Quantity, d.Rate, d.DiscountAmount, d.TaxPercent))
            .ToList()
        ?? new List<DocumentLine>());

    public List<PurchaseOrderDetailDto> ToDetails() =>
        Lines.Select(l => new PurchaseOrderDetailDto
        {
            ItemmasterId = l.ItemmasterId,
            Quantity = l.Quantity,
            Rate = l.Rate,
            DiscountAmount = l.DiscountAmount,
            TaxPercent = l.TaxPercent
        }).ToList();
}

public interface IHasPurchaseOrderData
{
    PurchaseOrderData Data { get; }
}

public static class PurchaseOrderRules
{
    public const int NumberMaxLength = 50;
    public const int NotesMaxLength = 500;
    public const int MaxPageSize = 500;
}

public abstract class PurchaseOrderDataValidator<T> : RuleValidator<T> where T : IHasPurchaseOrderData
{
    protected PurchaseOrderDataValidator()
    {
        Rule(c => c.Data.VendorId > 0, "VendorId", "Vendor is required.");
        Rule(c => c.Data.PODate != default, "PODate", "Purchase order date is required.");
        Rule(c => FieldChecks.MaxLength(c.Data.PONumber, PurchaseOrderRules.NumberMaxLength), "PONumber",
            $"PO number cannot be longer than {PurchaseOrderRules.NumberMaxLength} characters.");
        Rule(c => FieldChecks.MaxLength(c.Data.Notes, PurchaseOrderRules.NotesMaxLength), "Notes",
            $"Notes cannot be longer than {PurchaseOrderRules.NotesMaxLength} characters.");

        Custom(c => DocumentLineChecks.Validate(c.Data.Lines));
    }
}
