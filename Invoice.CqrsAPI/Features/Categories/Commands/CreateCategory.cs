using Invoice.BAL.Contracts;
using Invoice.CqrsAPI.Cqrs;
using Invoice.DTOs;

namespace Invoice.CqrsAPI.Features.Categories.Commands;

public sealed record CreateCategoryCommand(
    string Code,
    string Name,
    string? Description,
    bool? IsActive,
    string CreatedBy) : ICommand<int>, ICategoryFields;

public sealed class CreateCategoryValidator : CategoryFieldsValidator<CreateCategoryCommand>
{
}

public sealed class CreateCategoryHandler : ICommandHandler<CreateCategoryCommand, int>
{
    private readonly ICategoryService _categories;

    public CreateCategoryHandler(ICategoryService categories)
    {
        _categories = categories;
    }

    public async Task<int> HandleAsync(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var code = request.Code.Trim();

        await CategoryRules.EnsureCodeIsUniqueAsync(_categories, code, excludeId: null);

        var dto = new CategoryDto
        {
            Code = code,
            Name = request.Name.Trim(),
            Description = request.Description,
            IsActive = request.IsActive,
            CreatedBy = request.CreatedBy
        };

        return await _categories.AddAsync(dto);
    }
}
