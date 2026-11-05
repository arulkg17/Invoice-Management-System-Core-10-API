using Invoice.BAL.Contracts;
using Invoice.CqrsAPI.Cqrs;
using Invoice.DTOs;

namespace Invoice.CqrsAPI.Features.Itemmasters.Commands;

public sealed record CreateItemmasterCommand(ItemmasterData Data, string CreatedBy)
    : ICommand<int>, IHasItemmasterData;

public sealed class CreateItemmasterValidator : ItemmasterDataValidator<CreateItemmasterCommand>
{
}

public sealed class CreateItemmasterHandler : ICommandHandler<CreateItemmasterCommand, int>
{
    private readonly IItemmasterService _items;
    private readonly ICategoryService _categories;

    public CreateItemmasterHandler(IItemmasterService items, ICategoryService categories)
    {
        _items = items;
        _categories = categories;
    }

    public async Task<int> HandleAsync(CreateItemmasterCommand request, CancellationToken cancellationToken)
    {
        var data = request.Data;
        var itemCode = data.ItemCode.Trim();
        var barCode = data.ItemBarCode.Trim();

        await ItemmasterChecks.EnsureCategoryExistsAsync(_categories, data.CategoryId);
        await ItemmasterChecks.EnsureCodesAreUniqueAsync(_items, itemCode, barCode, excludeId: null);

        var dto = new ItemmasterDto
        {
            CategoryId = data.CategoryId,
            ItemBarCode = barCode,
            ItemCode = itemCode,
            ItemName = data.ItemName.Trim(),
            Description = data.Description,
            Uom = data.Uom.Trim(),
            Rate = data.Rate,
            MinimumStock = data.MinimumStock,
            MaximumStock = data.MaximumStock,
            IsActive = data.IsActive,
            CreatedBy = request.CreatedBy
        };

        return await _items.AddAsync(dto);
    }
}
