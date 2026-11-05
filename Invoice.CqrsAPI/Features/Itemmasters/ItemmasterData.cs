using Invoice.BAL.Contracts;
using Invoice.CqrsAPI.Common;
using Invoice.CqrsAPI.Cqrs;
using Invoice.DTOs;
using Invoice.Model;

namespace Invoice.CqrsAPI.Features.Itemmasters;

/// <summary>The editable fields of an item. Create and Update both carry one of these.</summary>
public sealed record ItemmasterData(
    int CategoryId,
    string ItemBarCode,
    string ItemCode,
    string ItemName,
    string? Description,
    string Uom,
    decimal? Rate,
    decimal? MinimumStock,
    decimal? MaximumStock,
    bool? IsActive)
{
    public static ItemmasterData From(ItemmasterDto dto) => new(
        dto.CategoryId, dto.ItemBarCode, dto.ItemCode, dto.ItemName, dto.Description,
        dto.Uom, dto.Rate, dto.MinimumStock, dto.MaximumStock, dto.IsActive);
}

public interface IHasItemmasterData
{
    ItemmasterData Data { get; }
}

/// <summary>Limits used by the validators. Match them to the Itemmaster table definition.</summary>
public static class ItemmasterRules
{
    public const int BarCodeMaxLength = 50;
    public const int CodeMaxLength = 50;
    public const int NameMaxLength = 200;
    public const int UomMaxLength = 20;
    public const int DescriptionMaxLength = 500;
    public const int MaxPageSize = 500;
}

public abstract class ItemmasterDataValidator<T> : RuleValidator<T> where T : IHasItemmasterData
{
    protected ItemmasterDataValidator()
    {
        Rule(c => c.Data.CategoryId > 0, "CategoryId", "Category is required.");

        Rule(c => !string.IsNullOrWhiteSpace(c.Data.ItemBarCode), "ItemBarCode", "Bar code is required.");
        Rule(c => FieldChecks.MaxLength(c.Data.ItemBarCode, ItemmasterRules.BarCodeMaxLength), "ItemBarCode",
            $"Bar code cannot be longer than {ItemmasterRules.BarCodeMaxLength} characters.");

        Rule(c => !string.IsNullOrWhiteSpace(c.Data.ItemCode), "ItemCode", "Item code is required.");
        Rule(c => FieldChecks.MaxLength(c.Data.ItemCode, ItemmasterRules.CodeMaxLength), "ItemCode",
            $"Item code cannot be longer than {ItemmasterRules.CodeMaxLength} characters.");

        Rule(c => !string.IsNullOrWhiteSpace(c.Data.ItemName), "ItemName", "Item name is required.");
        Rule(c => FieldChecks.MaxLength(c.Data.ItemName, ItemmasterRules.NameMaxLength), "ItemName",
            $"Item name cannot be longer than {ItemmasterRules.NameMaxLength} characters.");

        Rule(c => !string.IsNullOrWhiteSpace(c.Data.Uom), "Uom", "Unit of measure is required.");
        Rule(c => FieldChecks.MaxLength(c.Data.Uom, ItemmasterRules.UomMaxLength), "Uom",
            $"Unit of measure cannot be longer than {ItemmasterRules.UomMaxLength} characters.");

        Rule(c => FieldChecks.MaxLength(c.Data.Description, ItemmasterRules.DescriptionMaxLength), "Description",
            $"Description cannot be longer than {ItemmasterRules.DescriptionMaxLength} characters.");

        Rule(c => (c.Data.Rate ?? 0) >= 0, "Rate", "Rate cannot be negative.");
        Rule(c => (c.Data.MinimumStock ?? 0) >= 0, "MinimumStock", "Minimum stock cannot be negative.");
        Rule(c => (c.Data.MaximumStock ?? 0) >= 0, "MaximumStock", "Maximum stock cannot be negative.");
        Rule(c => c.Data.MinimumStock is null || c.Data.MaximumStock is null
                  || c.Data.MaximumStock >= c.Data.MinimumStock,
            "MaximumStock", "Maximum stock cannot be less than minimum stock.");
    }
}

/// <summary>Business rules that need the database. They live next to the use cases that share them.</summary>
public static class ItemmasterChecks
{
    public static async Task EnsureCategoryExistsAsync(ICategoryService categories, int categoryId)
    {
        _ = await categories.GetByIdAsync(categoryId)
            ?? throw new NotFoundException($"Category with Id {categoryId} was not found.");
    }

    /// <summary>Item code and bar code must both be unique.</summary>
    public static async Task EnsureCodesAreUniqueAsync(
        IItemmasterService items, string itemCode, string barCode, int? excludeId)
    {
        var all = (await items.GetAllAsync()).Where(x => x.Id != excludeId).ToList();

        if (all.Any(x => string.Equals(x.ItemCode?.Trim(), itemCode, StringComparison.OrdinalIgnoreCase)))
            throw new BusinessRuleException($"Item code '{itemCode}' already exists.");

        if (all.Any(x => string.Equals(x.ItemBarCode?.Trim(), barCode, StringComparison.OrdinalIgnoreCase)))
            throw new BusinessRuleException($"Bar code '{barCode}' already exists.");
    }
}
