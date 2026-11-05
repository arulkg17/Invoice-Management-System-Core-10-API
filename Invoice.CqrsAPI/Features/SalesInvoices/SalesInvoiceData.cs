using Invoice.CqrsAPI.Common;
using Invoice.CqrsAPI.Cqrs;
using Invoice.DTOs;

namespace Invoice.CqrsAPI.Features.SalesInvoices;

/// <summary>The editable part of a sales invoice. Totals, status and audit columns are NOT here: the BAL owns them.</summary>
public sealed record SalesInvoiceData(
    string? InvoiceNumber,
    DateTime InvoiceDate,
    DateTime? DueDate,
    int CustomerId,
    string? Notes,
    IReadOnlyList<DocumentLine> Lines)
{
    public static SalesInvoiceData From(SalesInvoiceDto dto) => new(
        dto.InvoiceNumber,
        dto.InvoiceDate,
        dto.DueDate,
        dto.CustomerId,
        dto.Notes,
        dto.Details?
            .Select(d => new DocumentLine(d.ItemmasterId, d.Quantity, d.Rate, d.DiscountAmount, d.TaxPercent))
            .ToList()
        ?? new List<DocumentLine>());

    public List<SalesInvoiceDetailDto> ToDetails() =>
        Lines.Select(l => new SalesInvoiceDetailDto
        {
            ItemmasterId = l.ItemmasterId,
            Quantity = l.Quantity,
            Rate = l.Rate,
            DiscountAmount = l.DiscountAmount,
            TaxPercent = l.TaxPercent
        }).ToList();
}

public interface IHasSalesInvoiceData
{
    SalesInvoiceData Data { get; }
}

public static class SalesInvoiceRules
{
    public const int NumberMaxLength = 50;
    public const int NotesMaxLength = 500;
    public const int MaxPageSize = 500;
}

public abstract class SalesInvoiceDataValidator<T> : RuleValidator<T> where T : IHasSalesInvoiceData
{
    protected SalesInvoiceDataValidator()
    {
        Rule(c => c.Data.CustomerId > 0, "CustomerId", "Customer is required.");
        Rule(c => c.Data.InvoiceDate != default, "InvoiceDate", "Invoice date is required.");
        Rule(c => c.Data.DueDate is null || c.Data.DueDate.Value.Date >= c.Data.InvoiceDate.Date, "DueDate",
            "Due date cannot be earlier than the invoice date.");
        Rule(c => FieldChecks.MaxLength(c.Data.InvoiceNumber, SalesInvoiceRules.NumberMaxLength), "InvoiceNumber",
            $"Invoice number cannot be longer than {SalesInvoiceRules.NumberMaxLength} characters.");
        Rule(c => FieldChecks.MaxLength(c.Data.Notes, SalesInvoiceRules.NotesMaxLength), "Notes",
            $"Notes cannot be longer than {SalesInvoiceRules.NotesMaxLength} characters.");

        Custom(c => DocumentLineChecks.Validate(c.Data.Lines));
    }
}
