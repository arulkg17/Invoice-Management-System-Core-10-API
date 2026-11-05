using Invoice.BAL.Contracts;
using Invoice.CqrsAPI.Cqrs;
using Invoice.Model;

namespace Invoice.CqrsAPI.Features.Itemmasters.Commands;

public sealed record UpdateItemmasterCommand(int Id, ItemmasterData Data, string UpdatedBy)
    : ICommand<Unit>, IHasItemmasterData;

public sealed class UpdateItemmasterValidator : ItemmasterDataValidator<UpdateItemmasterCommand>
{
    public UpdateItemmasterValidator()
    {
        Rule(c => c.Id > 0, "Id", "Id must be greater than zero.");
    }
}

public sealed class UpdateItemmasterHandler : ICommandHandler<UpdateItemmasterCommand, Unit>
{
    private readonly IItemmasterService _items;
    private readonly ICategoryService _categories;

    public UpdateItemmasterHandler(IItemmasterService items, ICategoryService categories)
    {
        _items = items;
        _categories = categories;
    }

    public async Task<Unit> HandleAsync(UpdateItemmasterCommand request, CancellationToken cancellationToken)
    {
        var existing = await _items.GetByIdAsync(request.Id)
            ?? throw new NotFoundException($"Item with Id {request.Id} was not found.");

        var data = request.Data;
        var itemCode = data.ItemCode.Trim();
        var barCode = data.ItemBarCode.Trim();

        if (data.CategoryId != existing.CategoryId)
        {
            await ItemmasterChecks.EnsureCategoryExistsAsync(_categories, data.CategoryId);
        }

        await ItemmasterChecks.EnsureCodesAreUniqueAsync(_items, itemCode, barCode, excludeId: request.Id);

        existing.CategoryId = data.CategoryId;
        existing.ItemBarCode = barCode;
        existing.ItemCode = itemCode;
        existing.ItemName = data.ItemName.Trim();
        existing.Description = data.Description;
        existing.Uom = data.Uom.Trim();
        existing.Rate = data.Rate;
        existing.MinimumStock = data.MinimumStock;
        existing.MaximumStock = data.MaximumStock;
        existing.IsActive = data.IsActive ?? existing.IsActive;
        existing.UpdatedBy = request.UpdatedBy;

        var updated = await _items.UpdateAsync(existing);

        if (!updated)
        {
            throw new NotFoundException($"Item with Id {request.Id} was not found.");
        }

        return Unit.Value;
    }
}
