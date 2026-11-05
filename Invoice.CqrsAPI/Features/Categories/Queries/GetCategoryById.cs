using Invoice.BAL.Contracts;
using Invoice.CqrsAPI.Cqrs;
using Invoice.DTOs;
using Invoice.Model;

namespace Invoice.CqrsAPI.Features.Categories.Queries;

public sealed record GetCategoryByIdQuery(int Id) : IQuery<CategoryDto>;

public sealed class GetCategoryByIdValidator : RuleValidator<GetCategoryByIdQuery>
{
    public GetCategoryByIdValidator()
    {
        Rule(q => q.Id > 0, "Id", "Id must be greater than zero.");
    }
}

public sealed class GetCategoryByIdHandler : IQueryHandler<GetCategoryByIdQuery, CategoryDto>
{
    private readonly ICategoryService _categories;

    public GetCategoryByIdHandler(ICategoryService categories)
    {
        _categories = categories;
    }

    public async Task<CategoryDto> HandleAsync(GetCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        return await _categories.GetByIdAsync(request.Id)
            ?? throw new NotFoundException($"Category with Id {request.Id} was not found.");
    }
}
