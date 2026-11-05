using Asp.Versioning;
using Invoice.CqrsAPI.Cqrs;
using Invoice.CqrsAPI.Features.Categories.Commands;
using Invoice.CqrsAPI.Features.Categories.Queries;
using Invoice.DTOs;
using Invoice.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Invoice.CqrsAPI.Controllers;

/// <summary>
/// Same routes and same JSON as Invoice.CoreAPI's CategoryController, so the Angular / React /
/// Blazor / WPF / WinForms clients only need a different base URL.
///
/// The controller is now THIN: build a command or query, hand it to the dispatcher, return the result.
/// </summary>
[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
[Authorize]
[ApiVersion(1.0)]
public class CategoryController : ApiControllerBase
{
    private readonly IDispatcher _dispatcher;

    public CategoryController(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    // =====================================================
    //  QUERIES (read side)
    // =====================================================

    // GET: api/v1/Category/GetAll
    [HttpGet("GetAll")]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var data = await _dispatcher.SendAsync(new GetAllCategoriesQuery(), cancellationToken);

        return Ok(Done("Categories retrieved successfully.", data));
    }

    // GET: api/v1/Category/GetById/5
    [HttpGet("GetById/{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var item = await _dispatcher.SendAsync(new GetCategoryByIdQuery(id), cancellationToken);

        return Ok(Done("Category retrieved successfully.", item));
    }

    // GET: api/v1/Category/GetAllPaged?Code=&Name=&pageNumber=1&pageSize=10
    [HttpGet("GetAllPaged")]
    public async Task<IActionResult> GetAllPaged(
        [FromQuery] string? Code,
        [FromQuery] string? Name,
        CancellationToken cancellationToken,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _dispatcher.SendAsync(
            new GetCategoriesPagedQuery(Code, Name, pageNumber, pageSize),
            cancellationToken);

        return Ok(new ApiResponse<PagedResultDto<CategoryDto>>
        {
            Success = true,
            Message = "Categories retrieved successfully.",
            Data = result,
            TotalRecords = result.TotalRecords
        });
    }

    // =====================================================
    //  COMMANDS (write side)
    // =====================================================

    // POST: api/v1/Category/Create
    [HttpPost("Create")]
    public async Task<IActionResult> Create(
        [FromBody] CategoryDto dto,
        CancellationToken cancellationToken)
    {
        var id = await _dispatcher.SendAsync(
            new CreateCategoryCommand(dto.Code, dto.Name, dto.Description, dto.IsActive, CurrentUser),
            cancellationToken);

        return Ok(Done("Category created successfully.", id));
    }

    // PUT: api/v1/Category/Update/5
    [HttpPut("Update/{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] CategoryDto dto,
        CancellationToken cancellationToken)
    {
        await _dispatcher.SendAsync(
            new UpdateCategoryCommand(id, dto.Code, dto.Name, dto.Description, dto.IsActive, CurrentUser),
            cancellationToken);

        return Ok(Done("Category updated successfully."));
    }

    // DELETE: api/v1/Category/Delete/5
    [HttpDelete("Delete/{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _dispatcher.SendAsync(new DeleteCategoryCommand(id), cancellationToken);

        return Ok(Done("Category deleted successfully."));
    }
}
