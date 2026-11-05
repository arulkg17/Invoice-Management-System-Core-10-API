using Invoice.BAL.Contracts;
using Invoice.CqrsAPI.Cqrs;
using Invoice.DTOs;
using Invoice.Model;

namespace Invoice.CqrsAPI.Features.Stock.Queries;

// Stock is read-only: it is changed only by posting receipts and sales invoices. So: queries only, no commands.

public sealed record GetAllStockQuery : IQuery<IEnumerable<ItemStockDto>>;

public sealed class GetAllStockHandler : IQueryHandler<GetAllStockQuery, IEnumerable<ItemStockDto>>
{
    private readonly IStockService _stock;

    public GetAllStockHandler(IStockService stock)
    {
        _stock = stock;
    }

    public Task<IEnumerable<ItemStockDto>> HandleAsync(GetAllStockQuery request, CancellationToken cancellationToken)
    {
        return _stock.GetAllAsync();
    }
}

public sealed record GetStockByItemQuery(int ItemmasterId) : IQuery<ItemStockDto>;

public sealed class GetStockByItemValidator : RuleValidator<GetStockByItemQuery>
{
    public GetStockByItemValidator()
    {
        Rule(q => q.ItemmasterId > 0, "ItemmasterId", "Item id must be greater than zero.");
    }
}

public sealed class GetStockByItemHandler : IQueryHandler<GetStockByItemQuery, ItemStockDto>
{
    private readonly IStockService _stock;

    public GetStockByItemHandler(IStockService stock)
    {
        _stock = stock;
    }

    public async Task<ItemStockDto> HandleAsync(GetStockByItemQuery request, CancellationToken cancellationToken)
    {
        return await _stock.GetByItemmasterIdAsync(request.ItemmasterId)
            ?? throw new NotFoundException($"Item with Id {request.ItemmasterId} was not found.");
    }
}
