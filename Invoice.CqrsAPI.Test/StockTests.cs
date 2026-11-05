using Invoice.BAL.Contracts;
using Invoice.CqrsAPI.Features.Stock.Queries;
using Invoice.DTOs;
using Invoice.Model;
using Moq;

namespace Invoice.CqrsAPI.Test;

public class StockTests
{
    private readonly Mock<IStockService> _stock = new();

    [Fact]
    public async Task GetByItem_returns_the_stock_row()
    {
        _stock.Setup(s => s.GetByItemmasterIdAsync(5))
            .ReturnsAsync(new ItemStockDto { ItemmasterId = 5, OnHandQuantity = 9 });

        var row = await new GetStockByItemHandler(_stock.Object)
            .HandleAsync(new GetStockByItemQuery(5), CancellationToken.None);

        Assert.Equal(9, row.OnHandQuantity);
    }

    [Fact]
    public async Task GetByItem_unknown_item_throws_not_found()
    {
        _stock.Setup(s => s.GetByItemmasterIdAsync(5)).ReturnsAsync((ItemStockDto?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            new GetStockByItemHandler(_stock.Object).HandleAsync(new GetStockByItemQuery(5), CancellationToken.None));
    }

    [Fact]
    public void GetByItem_rejects_a_zero_id()
    {
        var failures = new GetStockByItemValidator().Validate(new GetStockByItemQuery(0));

        Assert.Contains(failures, f => f.Property == "ItemmasterId");
    }
}
