using Invoice.BAL.Contracts;
using Invoice.CqrsAPI.Cqrs;
using Invoice.DTOs;

namespace Invoice.CqrsAPI.Features.Categories.Queries;

public sealed record GetCategoriesPagedQuery(
    string? Code,
    string? Name,
    int PageNumber = 1,
    int PageSize = 10) : IQuery<PagedResultDto<CategoryDto>>;

public sealed class GetCategoriesPagedValidator : RuleValidator<GetCategoriesPagedQuery>
{
    public GetCategoriesPagedValidator()
    {
        Rule(q => q.PageNumber >= 1, "PageNumber", "Page number must be greater than zero.");
        Rule(q => q.PageSize >= 1, "PageSize", "Page size must be greater than zero.");
        Rule(q => q.PageSize <= CategoryRules.MaxPageSize, "PageSize",
            $"Page size cannot be greater than {CategoryRules.MaxPageSize}.");
    }
}

public sealed class GetCategoriesPagedHandler
    : IQueryHandler<GetCategoriesPagedQuery, PagedResultDto<CategoryDto>>
{
    private readonly ICategoryService _categories;

    public GetCategoriesPagedHandler(ICategoryService categories)
    {
        _categories = categories;
    }

    public Task<PagedResultDto<CategoryDto>> HandleAsync(
        GetCategoriesPagedQuery request,
        CancellationToken cancellationToken)
    {
        return _categories.GetAllPagedAsync(
            request.Code,
            request.Name,
            request.PageNumber,
            request.PageSize);
    }
}
