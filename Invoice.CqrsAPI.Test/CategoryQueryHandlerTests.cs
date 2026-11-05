using Invoice.BAL.Contracts;
using Invoice.CqrsAPI.Features.Categories.Queries;
using Invoice.DTOs;
using Invoice.Model;
using Moq;

namespace Invoice.CqrsAPI.Test;

public class CategoryQueryHandlerTests
{
    private readonly Mock<ICategoryService> _categories = new();

    [Fact]
    public async Task GetAll_returns_what_the_service_returns()
    {
        var data = new List<CategoryDto> { new() { Id = 1, Code = "A", Name = "A" } }.AsEnumerable();
        _categories.Setup(s => s.GetAllAsync()).ReturnsAsync(data);

        var result = await new GetAllCategoriesHandler(_categories.Object)
            .HandleAsync(new GetAllCategoriesQuery(), CancellationToken.None);

        Assert.Single(result);
    }

    [Fact]
    public async Task GetById_returns_the_category()
    {
        _categories.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(new CategoryDto { Id = 1, Code = "A", Name = "A" });

        var result = await new GetCategoryByIdHandler(_categories.Object)
            .HandleAsync(new GetCategoryByIdQuery(1), CancellationToken.None);

        Assert.Equal(1, result.Id);
    }

    [Fact]
    public async Task GetById_unknown_id_throws_not_found()
    {
        _categories.Setup(s => s.GetByIdAsync(1)).ReturnsAsync((CategoryDto?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            new GetCategoryByIdHandler(_categories.Object)
                .HandleAsync(new GetCategoryByIdQuery(1), CancellationToken.None));
    }

    [Fact]
    public async Task GetPaged_passes_filters_and_paging_to_the_service()
    {
        _categories
            .Setup(s => s.GetAllPagedAsync("C", "Bo", 2, 5))
            .ReturnsAsync(new PagedResultDto<CategoryDto> { TotalRecords = 12 });

        var result = await new GetCategoriesPagedHandler(_categories.Object)
            .HandleAsync(new GetCategoriesPagedQuery("C", "Bo", 2, 5), CancellationToken.None);

        Assert.Equal(12, result.TotalRecords);
        _categories.Verify(s => s.GetAllPagedAsync("C", "Bo", 2, 5), Times.Once);
    }
}
