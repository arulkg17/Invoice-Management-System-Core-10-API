using Invoice.BAL.Contracts;
using Invoice.CqrsAPI.Cqrs;
using Invoice.CqrsAPI.Features.Categories.Commands;
using Invoice.CqrsAPI.Features.Categories.Queries;
using Invoice.DTOs;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Invoice.CqrsAPI.Test;

/// <summary>
/// End-to-end through the real Dispatcher + behaviors + DI scanning, with only the BAL services mocked.
/// </summary>
public class DispatcherPipelineTests
{
    private readonly Mock<ICategoryService> _categories = new();
    private readonly Mock<IItemmasterService> _items = new();

    private IDispatcher BuildDispatcher()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddSingleton<ICategoryService>(_categories.Object);
        services.AddSingleton<IItemmasterService>(_items.Object);
        services.AddCqrs(typeof(CreateCategoryCommand).Assembly);

        return services.BuildServiceProvider().CreateScope().ServiceProvider.GetRequiredService<IDispatcher>();
    }

    [Fact]
    public async Task Invalid_command_is_rejected_by_validation_and_never_reaches_the_service()
    {
        var dispatcher = BuildDispatcher();

        var ex = await Assert.ThrowsAsync<RequestValidationException>(() =>
            dispatcher.SendAsync(new CreateCategoryCommand("", "", null, true, "admin")));

        Assert.Contains(ex.Failures, f => f.Property == "Code");
        Assert.Contains(ex.Failures, f => f.Property == "Name");

        _categories.Verify(s => s.AddAsync(It.IsAny<CategoryDto>()), Times.Never);
        _categories.Verify(s => s.GetAllAsync(), Times.Never);
    }

    [Fact]
    public async Task Valid_command_flows_through_the_pipeline_to_the_handler()
    {
        _categories.Setup(s => s.GetAllAsync()).ReturnsAsync(Array.Empty<CategoryDto>().AsEnumerable());
        _categories.Setup(s => s.AddAsync(It.IsAny<CategoryDto>())).ReturnsAsync(5);

        var dispatcher = BuildDispatcher();

        var id = await dispatcher.SendAsync(new CreateCategoryCommand("C01", "Books", null, true, "admin"));

        Assert.Equal(5, id);
    }

    [Fact]
    public async Task Query_flows_through_the_pipeline_to_the_handler()
    {
        _categories
            .Setup(s => s.GetAllPagedAsync(null, null, 1, 10))
            .ReturnsAsync(new PagedResultDto<CategoryDto> { TotalRecords = 3 });

        var dispatcher = BuildDispatcher();

        var result = await dispatcher.SendAsync(new GetCategoriesPagedQuery(null, null));

        Assert.Equal(3, result.TotalRecords);
    }

    [Fact]
    public async Task Invalid_query_is_rejected_by_validation()
    {
        var dispatcher = BuildDispatcher();

        await Assert.ThrowsAsync<RequestValidationException>(() =>
            dispatcher.SendAsync(new GetCategoriesPagedQuery(null, null, PageNumber: 0)));
    }
}
