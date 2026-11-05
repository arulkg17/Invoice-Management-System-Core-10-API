using Invoice.BAL.Contracts;
using Invoice.CqrsAPI.Cqrs;
using Invoice.DTOs;
using Invoice.Model;

namespace Invoice.CqrsAPI.Features.Itemmasters.Queries;

// ---------------- GetAll ----------------

public sealed record GetAllItemmastersQuery : IQuery<IEnumerable<ItemmasterDto>>;

public sealed class GetAllItemmastersHandler : IQueryHandler<GetAllItemmastersQuery, IEnumerable<ItemmasterDto>>
{
    private readonly IItemmasterService _items;

    public GetAllItemmastersHandler(IItemmasterService items)
    {
        _items = items;
    }

    public Task<IEnumerable<ItemmasterDto>> HandleAsync(GetAllItemmastersQuery request, CancellationToken cancellationToken)
    {
        return _items.GetAllAsync();
    }
}

// ---------------- GetById ----------------

public sealed record GetItemmasterByIdQuery(int Id) : IQuery<ItemmasterDto>;

public sealed class GetItemmasterByIdValidator : RuleValidator<GetItemmasterByIdQuery>
{
    public GetItemmasterByIdValidator()
    {
        Rule(q => q.Id > 0, "Id", "Id must be greater than zero.");
    }
}

public sealed class GetItemmasterByIdHandler : IQueryHandler<GetItemmasterByIdQuery, ItemmasterDto>
{
    private readonly IItemmasterService _items;

    public GetItemmasterByIdHandler(IItemmasterService items)
    {
        _items = items;
    }

    public async Task<ItemmasterDto> HandleAsync(GetItemmasterByIdQuery request, CancellationToken cancellationToken)
    {
        return await _items.GetByIdAsync(request.Id)
            ?? throw new NotFoundException($"Item with Id {request.Id} was not found.");
    }
}

// ---------------- GetPaged ----------------

public sealed record GetItemmastersPagedQuery(
    int? CategoryId,
    string? ItemBarCode,
    string? ItemCode,
    string? ItemName,
    string? Uom,
    bool? IsActive,
    int PageNumber = 1,
    int PageSize = 10) : IQuery<PagedResultDto<ItemmasterDto>>;

public sealed class GetItemmastersPagedValidator : RuleValidator<GetItemmastersPagedQuery>
{
    public GetItemmastersPagedValidator()
    {
        Rule(q => q.PageNumber >= 1, "PageNumber", "Page number must be greater than zero.");
        Rule(q => q.PageSize >= 1, "PageSize", "Page size must be greater than zero.");
        Rule(q => q.PageSize <= ItemmasterRules.MaxPageSize, "PageSize",
            $"Page size cannot be greater than {ItemmasterRules.MaxPageSize}.");
    }
}

public sealed class GetItemmastersPagedHandler : IQueryHandler<GetItemmastersPagedQuery, PagedResultDto<ItemmasterDto>>
{
    private readonly IItemmasterService _items;

    public GetItemmastersPagedHandler(IItemmasterService items)
    {
        _items = items;
    }

    public Task<PagedResultDto<ItemmasterDto>> HandleAsync(
        GetItemmastersPagedQuery request,
        CancellationToken cancellationToken)
    {
        var filter = new ItemmasterFilterDto
        {
            CategoryId = request.CategoryId,
            ItemBarCode = request.ItemBarCode,
            ItemCode = request.ItemCode,
            ItemName = request.ItemName,
            Uom = request.Uom,
            IsActive = request.IsActive,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };

        return _items.GetAllPagedAsync(filter);
    }
}

// ---------------- Active item count per category ----------------

public sealed record GetActiveItemCountByCategoryQuery(int CategoryId) : IQuery<int>;

public sealed class GetActiveItemCountByCategoryValidator : RuleValidator<GetActiveItemCountByCategoryQuery>
{
    public GetActiveItemCountByCategoryValidator()
    {
        Rule(q => q.CategoryId > 0, "CategoryId", "Category id must be greater than zero.");
    }
}

public sealed class GetActiveItemCountByCategoryHandler : IQueryHandler<GetActiveItemCountByCategoryQuery, int>
{
    private readonly IItemmasterService _items;

    public GetActiveItemCountByCategoryHandler(IItemmasterService items)
    {
        _items = items;
    }

    public Task<int> HandleAsync(GetActiveItemCountByCategoryQuery request, CancellationToken cancellationToken)
    {
        return _items.GetActiveItemCountByCategoryAsync(request.CategoryId);
    }
}
