using Invoice.CqrsAPI.Features.Categories;
using Invoice.CqrsAPI.Features.Categories.Commands;
using Invoice.CqrsAPI.Features.Categories.Queries;

namespace Invoice.CqrsAPI.Test;

public class CategoryValidatorTests
{
    [Fact]
    public void Create_valid_command_has_no_failures()
    {
        var failures = new CreateCategoryValidator()
            .Validate(new CreateCategoryCommand("C01", "Books", "Printed books", true, "admin"));

        Assert.Empty(failures);
    }

    [Theory]
    [InlineData("", "Books", "Code")]
    [InlineData("   ", "Books", "Code")]
    [InlineData("C01", "", "Name")]
    [InlineData("C01", "  ", "Name")]
    public void Create_blank_code_or_name_fails(string code, string name, string expectedProperty)
    {
        var failures = new CreateCategoryValidator()
            .Validate(new CreateCategoryCommand(code, name, null, true, "admin"));

        Assert.Contains(failures, f => f.Property == expectedProperty);
    }

    [Fact]
    public void Create_code_longer_than_limit_fails()
    {
        var longCode = new string('X', CategoryRules.CodeMaxLength + 1);

        var failures = new CreateCategoryValidator()
            .Validate(new CreateCategoryCommand(longCode, "Books", null, true, "admin"));

        Assert.Contains(failures, f => f.Property == "Code");
    }

    [Fact]
    public void Update_with_zero_id_fails()
    {
        var failures = new UpdateCategoryValidator()
            .Validate(new UpdateCategoryCommand(0, "C01", "Books", null, true, "admin"));

        Assert.Contains(failures, f => f.Property == "Id");
    }

    [Theory]
    [InlineData(0, 10, "PageNumber")]
    [InlineData(1, 0, "PageSize")]
    [InlineData(1, CategoryRules.MaxPageSize + 1, "PageSize")]
    public void Paged_query_rejects_bad_paging(int pageNumber, int pageSize, string expectedProperty)
    {
        var failures = new GetCategoriesPagedValidator()
            .Validate(new GetCategoriesPagedQuery(null, null, pageNumber, pageSize));

        Assert.Contains(failures, f => f.Property == expectedProperty);
    }

    [Fact]
    public void Paged_query_with_defaults_is_valid()
    {
        var failures = new GetCategoriesPagedValidator()
            .Validate(new GetCategoriesPagedQuery(null, null));

        Assert.Empty(failures);
    }
}
