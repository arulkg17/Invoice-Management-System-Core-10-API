using Asp.Versioning;
using Invoice.CqrsAPI.Cqrs;
using Invoice.CqrsAPI.Features.PurchaseOrders;
using Invoice.CqrsAPI.Features.PurchaseOrders.Commands;
using Invoice.CqrsAPI.Features.PurchaseOrders.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Invoice.DTOs;

namespace Invoice.CqrsAPI.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion(1.0)]
[ApiController]
[Authorize]
public class PurchaseOrderController : ApiControllerBase
{
    private readonly IDispatcher _dispatcher;

    public PurchaseOrderController(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    // ===================== QUERIES =====================

    // GET: api/v1/PurchaseOrder/GetAll
    [HttpGet("GetAll")]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var data = await _dispatcher.SendAsync(new GetAllPurchaseOrdersQuery(), cancellationToken);

        return Ok(Done("Purchase Orders retrieved successfully", data));
    }

    // GET: api/v1/PurchaseOrder/GetById/1
    [HttpGet("GetById/{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var data = await _dispatcher.SendAsync(new GetPurchaseOrderByIdQuery(id), cancellationToken);

        return Ok(Done("Purchase Order retrieved successfully", data));
    }

    // GET: api/v1/PurchaseOrder/GetAllPaged?PONumber=&VendorId=&Status=&pageNumber=1&pageSize=10
    [HttpGet("GetAllPaged")]
    public async Task<IActionResult> GetAllPaged(
        string? PONumber,
        int? VendorId,
        string? Status,
        CancellationToken cancellationToken,
        int pageNumber = 1,
        int pageSize = 10)
    {
        var result = await _dispatcher.SendAsync(
            new GetPurchaseOrdersPagedQuery(PONumber, VendorId, Status, pageNumber, pageSize),
            cancellationToken);

        return Ok(Done("Purchase Orders retrieved successfully", result));
    }

    // GET: api/v1/PurchaseOrder/1/pdf
    [HttpGet("{id:int}/pdf")]
    [Produces("application/pdf")]
    public async Task<IActionResult> GetPdf(int id, CancellationToken cancellationToken)
    {
        var bytes = await _dispatcher.SendAsync(new GetPurchaseOrderPdfQuery(id), cancellationToken);

        return File(bytes, "application/pdf", $"PurchaseOrder-{id}.pdf");
    }

    // ===================== COMMANDS =====================

    // POST: api/v1/PurchaseOrder/Create
    [HttpPost("Create")]
    public async Task<IActionResult> Create(
        [FromBody] PurchaseOrderDto dto,
        CancellationToken cancellationToken)
    {
        var id = await _dispatcher.SendAsync(
            new CreatePurchaseOrderCommand(PurchaseOrderData.From(dto), CurrentUser),
            cancellationToken);

        return Ok(Done("Purchase Order created successfully", id));
    }

    // PUT: api/v1/PurchaseOrder/Update/1   (Draft only)
    [HttpPut("Update/{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] PurchaseOrderDto dto,
        CancellationToken cancellationToken)
    {
        await _dispatcher.SendAsync(
            new UpdatePurchaseOrderCommand(id, PurchaseOrderData.From(dto), CurrentUser),
            cancellationToken);

        return Ok(Done("Purchase Order updated successfully"));
    }

    // DELETE: api/v1/PurchaseOrder/Delete/1
    [HttpDelete("Delete/{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _dispatcher.SendAsync(new DeletePurchaseOrderCommand(id), cancellationToken);

        return Ok(Done("Purchase Order deleted successfully"));
    }

    // POST: api/v1/PurchaseOrder/Approve/1   (Draft -> Approved)
    [HttpPost("Approve/{id:int}")]
    public async Task<IActionResult> Approve(int id, CancellationToken cancellationToken)
    {
        await _dispatcher.SendAsync(new ApprovePurchaseOrderCommand(id, CurrentUser), cancellationToken);

        return Ok(Done("Purchase Order approved successfully"));
    }

    // POST: api/v1/PurchaseOrder/Cancel/1
    [HttpPost("Cancel/{id:int}")]
    public async Task<IActionResult> Cancel(int id, CancellationToken cancellationToken)
    {
        await _dispatcher.SendAsync(new CancelPurchaseOrderCommand(id, CurrentUser), cancellationToken);

        return Ok(Done("Purchase Order cancelled successfully"));
    }
}
