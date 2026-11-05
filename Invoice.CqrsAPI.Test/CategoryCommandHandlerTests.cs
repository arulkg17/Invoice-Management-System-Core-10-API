using Invoice.BAL.Contracts;
using Invoice.CqrsAPI.Features.Categories.Commands;
using Invoice.DTOs;
using Invoice.Model;
using Moq;

namespace Invoice.CqrsAPI.Test;

public class CategoryCommandHandlerTests
{
    private readonly Mock<ICategoryService> _categories = new();
    private readonly Mock<IItemmasterService> _items = new();

    private static IEnumerable<CategoryDto> NoCategories() => Array.Empty<CategoryDto>();

    // ---------------- Create ----------------

    [Fact]
    public async Task Create_saves_trimmed_category_and_returns_new_id()
    {
        _categories.Setup(s => s.GetAllAsync()).ReturnsAsync(NoCategories());
        _categories.Setup(s => s.AddAsync(It.IsAny<CategoryDto>())).ReturnsAsync(42);

        var handler = new CreateCategoryHandler(_categories.Object);

        var id = await handler.HandleAsync(
            new CreateCategoryCommand(" C01 ", " Books ", "Printed books", true, "admin"),
            CancellationToken.None);

        Assert.Equal(42, id);

        _categories.Verify(s => s.AddAsync(It.Is<CategoryDto>(d =>
            d.Code == "C01" &&
            d.Name == "Books" &&
            d.CreatedBy == "admin")), Times.Once);
    }

    [Fact]
    public async Task Create_with_duplicate_code_throws_business_rule_and_does_not_save()
    {
        _categories.Setup(s => s.GetAllAsync()).ReturnsAsync(new List<CategoryDto>
        {
            new() { Id = 1, Code = "c01", Name = "Existing" }
        }.AsEnumerable());

        var handler = new CreateCategoryHandler(_categories.Object);

        await Assert.ThrowsAsync<BusinessRuleException>(() => handler.HandleAsync(
            new CreateCategoryCommand("C01", "Books", null, true, "admin"),
            CancellationToken.None));

        _categories.Verify(s => s.AddAsync(It.IsAny<CategoryDto>()), Times.Never);
    }

    // ---------------- Update ----------------

    [Fact]
    public async Task Update_unknown_id_throws_not_found()
    {
        _categories.Setup(s => s.GetByIdAsync(7)).ReturnsAsync((CategoryDto?)null);

        var handler = new UpdateCategoryHandler(_categories.Object);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.HandleAsync(
            new UpdateCategoryCommand(7, "C01", "Books", null, true, "admin"),
            CancellationToken.None));

        _categories.Verify(s => s.UpdateAsync(It.IsAny<CategoryDto>()), Times.Never);
    }

    [Fact]
    public async Task Update_changes_fields_but_keeps_created_audit_values()
    {
        var existing = new CategoryDto
        {
            Id = 7,
            Code = "OLD",
            Name = "Old name",
            IsActive = true,
            CreatedBy = "creator",
            CreatedDate = new DateTime(2026, 1, 1)
        };

        _categories.Setup(s => s.GetByIdAsync(7)).ReturnsAsync(existing);
        _categories.Setup(s => s.GetAllAsync()).ReturnsAsync(new List<CategoryDto> { existing }.AsEnumerable());
        _categories.Setup(s => s.UpdateAsync(It.IsAny<CategoryDto>())).ReturnsAsync(true);

        var handler = new UpdateCategoryHandler(_categories.Object);

        await handler.HandleAsync(
            new UpdateCategoryCommand(7, "NEW", "New name", "desc", null, "editor"),
            CancellationToken.None);

        _categories.Verify(s => s.UpdateAsync(It.Is<CategoryDto>(d =>
            d.Id == 7 &&
            d.Code == "NEW" &&
            d.Name == "New name" &&
            d.IsActive == true &&              // null in the command keeps the old value
            d.UpdatedBy == "editor" &&
            d.CreatedBy == "creator" &&
            d.CreatedDate == new DateTime(2026, 1, 1))), Times.Once);
    }

    [Fact]
    public async Task Update_to_a_code_used_by_another_category_throws_business_rule()
    {
        var current = new CategoryDto { Id = 7, Code = "A", Name = "Current" };
        var other = new CategoryDto { Id = 8, Code = "B", Name = "Other" };

        _categories.Setup(s => s.GetByIdAsync(7)).ReturnsAsync(current);
        _categories.Setup(s => s.GetAllAsync()).ReturnsAsync(new List<CategoryDto> { current, other }.AsEnumerable());

        var handler = new UpdateCategoryHandler(_categories.Object);

        await Assert.ThrowsAsync<BusinessRuleException>(() => handler.HandleAsync(
            new UpdateCategoryCommand(7, "B", "Current", null, true, "editor"),
            CancellationToken.None));

        _categories.Verify(s => s.UpdateAsync(It.IsAny<CategoryDto>()), Times.Never);
    }

    // ---------------- Delete ----------------

    [Fact]
    public async Task Delete_unknown_id_throws_not_found()
    {
        _categories.Setup(s => s.GetByIdAsync(9)).ReturnsAsync((CategoryDto?)null);

        var handler = new DeleteCategoryHandler(_categories.Object, _items.Object);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.HandleAsync(new DeleteCategoryCommand(9), CancellationToken.None));
    }

    [Fact]
    public async Task Delete_category_with_active_items_throws_business_rule()
    {
        _categories.Setup(s => s.GetByIdAsync(9)).ReturnsAsync(new CategoryDto { Id = 9, Code = "C", Name = "N" });
        _items.Setup(s => s.GetActiveItemCountByCategoryAsync(9)).ReturnsAsync(3);

        var handler = new DeleteCategoryHandler(_categories.Object, _items.Object);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            handler.HandleAsync(new DeleteCategoryCommand(9), CancellationToken.None));

        _categories.Verify(s => s.DeleteAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Delete_empty_category_calls_service()
    {
        _categories.Setup(s => s.GetByIdAsync(9)).ReturnsAsync(new CategoryDto { Id = 9, Code = "C", Name = "N" });
        _items.Setup(s => s.GetActiveItemCountByCategoryAsync(9)).ReturnsAsync(0);
        _categories.Setup(s => s.DeleteAsync(9)).ReturnsAsync(true);

        var handler = new DeleteCategoryHandler(_categories.Object, _items.Object);

        await handler.HandleAsync(new DeleteCategoryCommand(9), CancellationToken.None);

        _categories.Verify(s => s.DeleteAsync(9), Times.Once);
    }
}
