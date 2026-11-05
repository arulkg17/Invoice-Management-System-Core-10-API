using Invoice.CqrsAPI.Common;
using Invoice.CqrsAPI.Cqrs;
using Invoice.DTOs;

namespace Invoice.CqrsAPI.Features.Receipts;

/// <summary>One purchase-order line being received. Item, rate and tax come from the PO line, never from the client.</summary>
public sealed record ReceiptLine(int PurchaseOrderDetailId, decimal ReceivedQuantity);

public sealed record ReceiptData(
    string? ReceiptNumber,
    DateTime ReceiptDate,
    int PurchaseOrderId,
    string? Notes,
    IReadOnlyList<ReceiptLine> Lines)
{
    public static ReceiptData From(ReceiptDto dto) => new(
        dto.ReceiptNumber,
        dto.ReceiptDate,
        dto.PurchaseOrderId,
        dto.Notes,
        dto.Details?
            .Select(d => new ReceiptLine(d.PurchaseOrderDetailId, d.ReceivedQuantity))
            .ToList()
        ?? new List<ReceiptLine>());

    public List<ReceiptDetailDto> ToDetails() =>
        Lines.Select(l => new ReceiptDetailDto
        {
            PurchaseOrderDetailId = l.PurchaseOrderDetailId,
            ReceivedQuantity = l.ReceivedQuantity
        }).ToList();
}

public interface IHasReceiptData
{
    ReceiptData Data { get; }
}

public static class ReceiptRules
{
    public const int NumberMaxLength = 50;
    public const int NotesMaxLength = 500;
    public const int MaxPageSize = 500;
}

public abstract class ReceiptDataValidator<T> : RuleValidator<T> where T : IHasReceiptData
{
    protected ReceiptDataValidator()
    {
        Rule(c => c.Data.PurchaseOrderId > 0, "PurchaseOrderId", "Purchase order is required.");
        Rule(c => c.Data.ReceiptDate != default, "ReceiptDate", "Receipt date is required.");
        Rule(c => FieldChecks.MaxLength(c.Data.ReceiptNumber, ReceiptRules.NumberMaxLength), "ReceiptNumber",
            $"Receipt number cannot be longer than {ReceiptRules.NumberMaxLength} characters.");
        Rule(c => FieldChecks.MaxLength(c.Data.Notes, ReceiptRules.NotesMaxLength), "Notes",
            $"Notes cannot be longer than {ReceiptRules.NotesMaxLength} characters.");

        Custom(c => CheckLines(c.Data.Lines));
    }

    private static IEnumerable<ValidationFailure> CheckLines(IReadOnlyList<ReceiptLine>? lines)
    {
        if (lines is null || lines.Count == 0)
        {
            yield return new ValidationFailure("Lines", "At least one line is required.");
            yield break;
        }

        for (var i = 0; i < lines.Count; i++)
        {
            if (lines[i].PurchaseOrderDetailId <= 0)
                yield return new ValidationFailure($"Lines[{i}].PurchaseOrderDetailId", "Purchase order line is required.");

            if (lines[i].ReceivedQuantity <= 0)
                yield return new ValidationFailure($"Lines[{i}].ReceivedQuantity", "Received quantity must be greater than zero.");
        }

        if (lines.GroupBy(l => l.PurchaseOrderDetailId).Any(g => g.Count() > 1))
            yield return new ValidationFailure("Lines", "Each purchase order line can appear only once per receipt.");
    }
}
