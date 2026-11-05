using Invoice.CqrsAPI.Cqrs;

namespace Invoice.CqrsAPI.Common;

/// <summary>One priced line, shared by Purchase Order and Sales Invoice requests.</summary>
public sealed record DocumentLine(
    int ItemmasterId,
    decimal Quantity,
    decimal Rate,
    decimal DiscountAmount,
    decimal TaxPercent);

public static class DocumentLineChecks
{
    /// <summary>Shape checks for the lines. The BAL repeats the money rules as the final safety net.</summary>
    public static IEnumerable<ValidationFailure> Validate(IReadOnlyList<DocumentLine>? lines)
    {
        if (lines is null || lines.Count == 0)
        {
            yield return new ValidationFailure("Lines", "At least one line is required.");
            yield break;
        }

        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            var name = $"Lines[{i}]";

            if (line.ItemmasterId <= 0)
                yield return new ValidationFailure($"{name}.ItemmasterId", "Item is required.");

            if (line.Quantity <= 0)
                yield return new ValidationFailure($"{name}.Quantity", "Quantity must be greater than zero.");

            if (line.Rate < 0)
                yield return new ValidationFailure($"{name}.Rate", "Rate cannot be negative.");

            if (line.DiscountAmount < 0)
                yield return new ValidationFailure($"{name}.DiscountAmount", "Discount cannot be negative.");

            if (line.TaxPercent < 0 || line.TaxPercent > 100)
                yield return new ValidationFailure($"{name}.TaxPercent", "Tax percent must be between 0 and 100.");

            if (line.Quantity > 0 && line.Rate >= 0 && line.DiscountAmount > line.Quantity * line.Rate)
                yield return new ValidationFailure($"{name}.DiscountAmount", "Discount cannot exceed the line amount.");
        }
    }
}
