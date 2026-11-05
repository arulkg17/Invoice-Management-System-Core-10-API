using Invoice.BAL.Contracts;
using Invoice.CqrsAPI.Cqrs;
using Invoice.Model;

namespace Invoice.CqrsAPI.Features.Categories.Commands;

public sealed record DeleteCategoryCommand(int Id) : ICommand<Unit>;

public sealed class DeleteCategoryValidator : RuleValidator<DeleteCategoryCommand>
{
    public DeleteCategoryValidator()
    {
        Rule(c => c.Id > 0, "Id", "Id must be greater than zero.");
    }
}

public sealed class DeleteCategoryHandler : ICommandHandler<DeleteCategoryCommand, Unit>
{
    private readonly ICategoryService _categories;
    private readonly IItemmasterService _items;

    public DeleteCategoryHandler(ICategoryService categories, IItemmasterService items)
    {
        _categories = categories;
        _items = items;
    }

    public async Task<Unit> HandleAsync(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        _ = await _categories.GetByIdAsync(request.Id)
            ?? throw new NotFoundException($"Category with Id {request.Id} was not found.");

        // Business rule: a category that still has active items cannot be deleted.
        var activeItems = await _items.GetActiveItemCountByCategoryAsync(request.Id);

        if (activeItems > 0)
        {
            throw new BusinessRuleException(
                $"Category cannot be deleted because it still has {activeItems} active item(s).");
        }

        var deleted = await _categories.DeleteAsync(request.Id);

        if (!deleted)
        {
            throw new NotFoundException($"Category with Id {request.Id} was not found.");
        }

        return Unit.Value;
    }
}
