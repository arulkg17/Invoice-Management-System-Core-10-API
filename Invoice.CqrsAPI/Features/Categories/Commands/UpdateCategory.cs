using Invoice.BAL.Contracts;
using Invoice.CqrsAPI.Cqrs;
using Invoice.Model;

namespace Invoice.CqrsAPI.Features.Categories.Commands;

public sealed record UpdateCategoryCommand(
    int Id,
    string Code,
    string Name,
    string? Description,
    bool? IsActive,
    string UpdatedBy) : ICommand<Unit>, ICategoryFields;

public sealed class UpdateCategoryValidator : CategoryFieldsValidator<UpdateCategoryCommand>
{
    public UpdateCategoryValidator()
    {
        Rule(c => c.Id > 0, "Id", "Id must be greater than zero.");
    }
}

public sealed class UpdateCategoryHandler : ICommandHandler<UpdateCategoryCommand, Unit>
{
    private readonly ICategoryService _categories;

    public UpdateCategoryHandler(ICategoryService categories)
    {
        _categories = categories;
    }

    public async Task<Unit> HandleAsync(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        // Load -> change -> save. Loading first keeps CreatedBy / CreatedDate intact
        // and gives us a clean "not found" instead of a silent no-op.
        var existing = await _categories.GetByIdAsync(request.Id)
            ?? throw new NotFoundException($"Category with Id {request.Id} was not found.");

        var code = request.Code.Trim();

        await CategoryRules.EnsureCodeIsUniqueAsync(_categories, code, excludeId: request.Id);

        existing.Code = code;
        existing.Name = request.Name.Trim();
        existing.Description = request.Description;
        existing.IsActive = request.IsActive ?? existing.IsActive;
        existing.UpdatedBy = request.UpdatedBy;

        var updated = await _categories.UpdateAsync(existing);

        if (!updated)
        {
            throw new NotFoundException($"Category with Id {request.Id} was not found.");
        }

        return Unit.Value;
    }
}
