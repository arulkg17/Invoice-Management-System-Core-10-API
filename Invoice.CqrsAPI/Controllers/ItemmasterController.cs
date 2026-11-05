using Asp.Versioning;
using Invoice.CqrsAPI.Cqrs;
using Invoice.CqrsAPI.Features.Itemmasters;
using Invoice.CqrsAPI.Features.Itemmasters.Commands;
using Invoice.CqrsAPI.Features.Itemmasters.Queries;
using Invoice.DTOs;
using Invoice.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Invoice.CqrsAPI.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion(1.0)]
[ApiController]
[Authorize]
public class ItemmasterController : ApiControllerBase
{
    private readonly IDispatcher _dispatcher;

    public ItemmasterController(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    // ===================== QUERIES =====================

    // GET: api/v1/Itemmaster/GetAllPaged?CategoryId=&ItemCode=&ItemName=&Uom=&IsActive=&PageNumber=1&PageSize=10
    [HttpGet("GetAllPaged")]
    public async Task<IActionResult> GetAllPaged(
        [FromQuery] ItemmasterFilterDto search,
        CancellationToken cancellationToken)
    {
        var result = await _dispatcher.SendAsync(
            new GetItemmastersPagedQuery(
                search.CategoryId, search.ItemBarCode, search.ItemCode, search.ItemName,
                search.Uom, search.IsActive, search.PageNumber, search.PageSize),
            cancellationToken);

        return Ok(new ApiResponse<PagedResultDto<ItemmasterDto>>
        {
            Success = true,
            Message = "Items retrieved successfully.",
            Data = result,
            TotalRecords = result.TotalRecords
        });
    }

    // GET: api/v1/Itemmaster/GetAll
    [HttpGet("GetAll")]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var data = await _dispatcher.SendAsync(new GetAllItemmastersQuery(), cancellationToken);

        return Ok(Done("Items retrieved successfully.", data));
    }

    // GET: api/v1/Itemmaster/GetById/5
    [HttpGet("GetById/{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var item = await _dispatcher.SendAsync(new GetItemmasterByIdQuery(id), cancellationToken);

        return Ok(Done("Item retrieved successfully.", item));
    }

    // GET: api/v1/Itemmaster/GetActiveItemCountByCategory/1
    [HttpGet("GetActiveItemCountByCategory/{categoryId:int}")]
    public async Task<IActionResult> GetActiveItemCountByCategory(
        int categoryId,
        CancellationToken cancellationToken)
    {
        var count = await _dispatcher.SendAsync(
            new GetActiveItemCountByCategoryQuery(categoryId), cancellationToken);

        return Ok(Done("Active item count retrieved successfully.", count));
    }

    // ===================== COMMANDS =====================

    // POST: api/v1/Itemmaster/Create
    [HttpPost("Create")]
    public async Task<IActionResult> Create(
        [FromBody] ItemmasterDto dto,
        CancellationToken cancellationToken)
    {
        var id = await _dispatcher.SendAsync(
            new CreateItemmasterCommand(ItemmasterData.From(dto), CurrentUser),
            cancellationToken);

        return Ok(Done("Item created successfully.", id));
    }

    // PUT: api/v1/Itemmaster/Update/5
    [HttpPut("Update/{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] ItemmasterDto dto,
        CancellationToken cancellationToken)
    {
        await _dispatcher.SendAsync(
            new UpdateItemmasterCommand(id, ItemmasterData.From(dto), CurrentUser),
            cancellationToken);

        return Ok(Done("Item updated successfully."));
    }

    // DELETE: api/v1/Itemmaster/Delete/5
    [HttpDelete("Delete/{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _dispatcher.SendAsync(new DeleteItemmasterCommand(id), cancellationToken);

        return Ok(Done("Item deleted successfully."));
    }
}
