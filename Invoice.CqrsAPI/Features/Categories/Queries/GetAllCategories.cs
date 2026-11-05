using Invoice.BAL.Contracts;
using Invoice.CqrsAPI.Cqrs;
using Invoice.DTOs;

namespace Invoice.CqrsAPI.Features.Categories.Queries;

public sealed record GetAllCategoriesQuery : IQuery<IEnumerable<CategoryDto>>;

public sealed class GetAllCategoriesHandler : IQueryHandler<GetAllCategoriesQuery, IEnumerable<CategoryDto>>
{
    private readonly ICategoryService _categories;

    public GetAllCategoriesHandler(ICategoryService categories)
    {
        _categories = categories;
    }

    public Task<IEnumerable<CategoryDto>> HandleAsync(
        GetAllCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        return _categories.GetAllAsync();
    }
}
