using Invoice.BAL.Contracts;
using Invoice.CqrsAPI.Features.Itemmasters;
using Invoice.CqrsAPI.Features.Itemmasters.Commands;
using Invoice.DTOs;
using Invoice.Model;
using Moq;

namespace Invoice.CqrsAPI.Test;

public class ItemmasterTests
{
    private readonly Mock<IItemmasterService> _items = new();
    private readonly Mock<ICategoryService> _categories = new();
    private readonly Mock<IStockService> _stock = new();

    private static ItemmasterData Data(
        int categoryId = 1, string code = "I1", string barCode = "B1",
        decimal? min = 1, decimal? max = 100, decimal? rate = 10)
        => new(categoryId, barCode, code, "Pen", null, "Nos", rate, min, max, true);

    // ---------------- validation ----------------

    [Fact]
    public void Valid_item_has_no_failures()
    {
        Assert.Empty(new CreateItemmasterValidator().Validate(new CreateItemmasterCommand(Data(), "admin")));
    }

    [Fact]
    public void Maximum_stock_below_minimum_fails()
    {
        var failures = new CreateItemmasterValidator()
            .Validate(new CreateItemmasterCommand(Data(min: 50, max: 10), "admin"));

        Assert.Contains(failures, f => f.Property == "MaximumStock");
    }

    [Fact]
    public void Negative_rate_and_missing_category_fail()
    {
        var failures = new CreateItemmasterValidator()
            .Validate(new CreateItemmasterCommand(Data(categoryId: 0, rate: -1), "admin"));

        Assert.Contains(failures, f => f.Property == "Rate");
        Assert.Contains(failures, f => f.Property == "CategoryId");
    }

    // ---------------- create ----------------

    [Fact]
    public async Task Create_with_unknown_category_throws_not_found()
    {
        _categories.Setup(c => c.GetByIdAsync(1)).ReturnsAsync((CategoryDto?)null);

        var handler = new CreateItemmasterHandler(_items.Object, _categories.Object);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.HandleAsync(new CreateItemmasterCommand(Data(), "admin"), CancellationToken.None));

        _items.Verify(i => i.AddAsync(It.IsAny<ItemmasterDto>()), Times.Never);
    }

    [Fact]
    public async Task Create_with_duplicate_item_code_throws_business_rule()
    {
        _categories.Setup(c => c.GetByIdAsync(1)).ReturnsAsync(new CategoryDto { Id = 1, Code = "C", Name = "N" });
        _items.Setup(i => i.GetAllAsync()).ReturnsAsync(new List<ItemmasterDto>
        {
            new() { Id = 9, ItemCode = "i1", ItemBarCode = "OTHER", ItemName = "x", Uom = "Nos" }
        }.AsEnumerable());

        var handler = new CreateItemmasterHandler(_items.Object, _categories.Object);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            handler.HandleAsync(new CreateItemmasterCommand(Data(), "admin"), CancellationToken.None));
    }

    [Fact]
    public async Task Create_saves_and_returns_id()
    {
        _categories.Setup(c => c.GetByIdAsync(1)).ReturnsAsync(new CategoryDto { Id = 1, Code = "C", Name = "N" });
        _items.Setup(i => i.GetAllAsync()).ReturnsAsync(Array.Empty<ItemmasterDto>().AsEnumerable());
        _items.Setup(i => i.AddAsync(It.IsAny<ItemmasterDto>())).ReturnsAsync(21);

        var id = await new CreateItemmasterHandler(_items.Object, _categories.Object)
            .HandleAsync(new CreateItemmasterCommand(Data(), "admin"), CancellationToken.None);

        Assert.Equal(21, id);
        _items.Verify(i => i.AddAsync(It.Is<ItemmasterDto>(d =>
            d.ItemCode == "I1" && d.CategoryId == 1 && d.CreatedBy == "admin")), Times.Once);
    }

    // ---------------- update ----------------

    [Fact]
    public async Task Update_unchanged_category_does_not_look_it_up_and_keeps_audit_values()
    {
        var existing = new ItemmasterDto
        {
            Id = 5, CategoryId = 1, ItemCode = "OLD", ItemBarCode = "OLDB", ItemName = "Old", Uom = "Nos",
            IsActive = true, CreatedBy = "creator"
        };

        _items.Setup(i => i.GetByIdAsync(5)).ReturnsAsync(existing);
        _items.Setup(i => i.GetAllAsync()).ReturnsAsync(new List<ItemmasterDto> { existing }.AsEnumerable());
        _items.Setup(i => i.UpdateAsync(It.IsAny<ItemmasterDto>())).ReturnsAsync(true);

        await new UpdateItemmasterHandler(_items.Object, _categories.Object)
            .HandleAsync(new UpdateItemmasterCommand(5, Data(), "editor"), CancellationToken.None);

        _categories.Verify(c => c.GetByIdAsync(It.IsAny<int>()), Times.Never);
        _items.Verify(i => i.UpdateAsync(It.Is<ItemmasterDto>(d =>
            d.ItemCode == "I1" && d.UpdatedBy == "editor" && d.CreatedBy == "creator")), Times.Once);
    }

    [Fact]
    public async Task Update_unknown_id_throws_not_found()
    {
        _items.Setup(i => i.GetByIdAsync(5)).ReturnsAsync((ItemmasterDto?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            new UpdateItemmasterHandler(_items.Object, _categories.Object)
                .HandleAsync(new UpdateItemmasterCommand(5, Data(), "editor"), CancellationToken.None));
    }

    // ---------------- delete ----------------

    [Fact]
    public async Task Delete_item_with_stock_on_hand_throws_business_rule()
    {
        _items.Setup(i => i.GetByIdAsync(5)).ReturnsAsync(new ItemmasterDto { Id = 5 });
        _stock.Setup(s => s.GetByItemmasterIdAsync(5)).ReturnsAsync(new ItemStockDto { ItemmasterId = 5, OnHandQuantity = 12 });

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            new DeleteItemmasterHandler(_items.Object, _stock.Object)
                .HandleAsync(new DeleteItemmasterCommand(5), CancellationToken.None));

        _items.Verify(i => i.DeleteAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Delete_item_without_stock_calls_service()
    {
        _items.Setup(i => i.GetByIdAsync(5)).ReturnsAsync(new ItemmasterDto { Id = 5 });
        _stock.Setup(s => s.GetByItemmasterIdAsync(5)).ReturnsAsync(new ItemStockDto { ItemmasterId = 5, OnHandQuantity = 0 });
        _items.Setup(i => i.DeleteAsync(5)).ReturnsAsync(true);

        await new DeleteItemmasterHandler(_items.Object, _stock.Object)
            .HandleAsync(new DeleteItemmasterCommand(5), CancellationToken.None);

        _items.Verify(i => i.DeleteAsync(5), Times.Once);
    }
}
