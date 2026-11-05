using Asp.Versioning;
using Invoice.CqrsAPI.Cqrs;
using Invoice.CqrsAPI.Features.Stock.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Invoice.CqrsAPI.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion(1.0)]
[ApiController]
[Authorize]
public class StockController : ApiControllerBase
{
    private readonly IDispatcher _dispatcher;

    public StockController(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    // GET: api/v1/Stock/GetAll
    [HttpGet("GetAll")]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var data = await _dispatcher.SendAsync(new GetAllStockQuery(), cancellationToken);

        return Ok(Done("Stock retrieved successfully", data));
    }

    // GET: api/v1/Stock/GetByItem/5
    [HttpGet("GetByItem/{itemmasterId:int}")]
    public async Task<IActionResult> GetByItem(int itemmasterId, CancellationToken cancellationToken)
    {
        var data = await _dispatcher.SendAsync(new GetStockByItemQuery(itemmasterId), cancellationToken);

        return Ok(Done("Stock retrieved successfully", data));
    }
}
