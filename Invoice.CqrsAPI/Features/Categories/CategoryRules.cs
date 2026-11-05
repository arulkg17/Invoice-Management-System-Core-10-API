using Invoice.BAL.Contracts;
using Invoice.CqrsAPI.Cqrs;
using Invoice.Model;

namespace Invoice.CqrsAPI.Features.Categories;

/// <summary>Limits used by the validators. Match them to the Category table definition.</summary>
public static class CategoryRules
{
    public const int CodeMaxLength = 50;
    public const int NameMaxLength = 200;
    public const int DescriptionMaxLength = 500;
    public const int MaxPageSize = 500;

    /// <summary>Business rule shared by Create and Update: a category Code must be unique.</summary>
    public static async Task EnsureCodeIsUniqueAsync(ICategoryService categories, string code, int? excludeId)
    {
        var all = await categories.GetAllAsync();

        var duplicate = all.Any(c =>
            string.Equals(c.Code?.Trim(), code, StringComparison.OrdinalIgnoreCase) &&
            c.Id != excludeId);

        if (duplicate)
        {
            throw new BusinessRuleException($"Category code '{code}' already exists.");
        }
    }
}

/// <summary>The fields Create and Update have in common, so their validators can share the same rules.</summary>
public interface ICategoryFields
{
    string Code { get; }
    string Name { get; }
    string? Description { get; }
}

public abstract class CategoryFieldsValidator<T> : RuleValidator<T> where T : ICategoryFields
{
    protected CategoryFieldsValidator()
    {
        Rule(c => !string.IsNullOrWhiteSpace(c.Code), "Code", "Code is required.");
        Rule(c => (c.Code?.Trim().Length ?? 0) <= CategoryRules.CodeMaxLength, "Code",
            $"Code cannot be longer than {CategoryRules.CodeMaxLength} characters.");

        Rule(c => !string.IsNullOrWhiteSpace(c.Name), "Name", "Name is required.");
        Rule(c => (c.Name?.Trim().Length ?? 0) <= CategoryRules.NameMaxLength, "Name",
            $"Name cannot be longer than {CategoryRules.NameMaxLength} characters.");

        Rule(c => (c.Description?.Length ?? 0) <= CategoryRules.DescriptionMaxLength, "Description",
            $"Description cannot be longer than {CategoryRules.DescriptionMaxLength} characters.");
    }
}
