using Asp.Versioning;
using Invoice.CqrsAPI.Cqrs;
using Invoice.CqrsAPI.Features.Receipts;
using Invoice.CqrsAPI.Features.Receipts.Commands;
using Invoice.CqrsAPI.Features.Receipts.Queries;
using Invoice.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Invoice.CqrsAPI.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion(1.0)]
[ApiController]
[Authorize]
public class ReceiptController : ApiControllerBase
{
    private readonly IDispatcher _dispatcher;

    public ReceiptController(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    // ===================== QUERIES =====================

    // GET: api/v1/Receipt/GetAll
    [HttpGet("GetAll")]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var data = await _dispatcher.SendAsync(new GetAllReceiptsQuery(), cancellationToken);

        return Ok(Done("Receipts retrieved successfully", data));
    }

    // GET: api/v1/Receipt/GetById/1
    [HttpGet("GetById/{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var data = await _dispatcher.SendAsync(new GetReceiptByIdQuery(id), cancellationToken);

        return Ok(Done("Receipt retrieved successfully", data));
    }

    // GET: api/v1/Receipt/GetAllPaged?receiptNumber=&purchaseOrderId=&vendorId=&status=&pageNumber=1&pageSize=10
    [HttpGet("GetAllPaged")]
    public async Task<IActionResult> GetAllPaged(
        string? receiptNumber,
        int? purchaseOrderId,
        int? vendorId,
        string? status,
        CancellationToken cancellationToken,
        int pageNumber = 1,
        int pageSize = 10)
    {
        var result = await _dispatcher.SendAsync(
            new GetReceiptsPagedQuery(receiptNumber, purchaseOrderId, vendorId, status, pageNumber, pageSize),
            cancellationToken);

        return Ok(Done("Receipts retrieved successfully", result));
    }

    // GET: api/v1/Receipt/1/pdf
    [HttpGet("{id:int}/pdf")]
    [Produces("application/pdf")]
    public async Task<IActionResult> GetPdf(int id, CancellationToken cancellationToken)
    {
        var bytes = await _dispatcher.SendAsync(new GetReceiptPdfQuery(id), cancellationToken);

        return File(bytes, "application/pdf", $"Receipt-{id}.pdf");
    }

    // ===================== COMMANDS =====================

    // POST: api/v1/Receipt/Create
    [HttpPost("Create")]
    public async Task<IActionResult> Create(
        [FromBody] ReceiptDto dto,
        CancellationToken cancellationToken)
    {
        var id = await _dispatcher.SendAsync(
            new CreateReceiptCommand(ReceiptData.From(dto), CurrentUser),
            cancellationToken);

        return Ok(Done("Receipt created successfully", id));
    }

    // PUT: api/v1/Receipt/Update/1   (Draft only)
    [HttpPut("Update/{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] ReceiptDto dto,
        CancellationToken cancellationToken)
    {
        await _dispatcher.SendAsync(
            new UpdateReceiptCommand(id, ReceiptData.From(dto), CurrentUser),
            cancellationToken);

        return Ok(Done("Receipt updated successfully"));
    }

    // DELETE: api/v1/Receipt/Delete/1
    [HttpDelete("Delete/{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _dispatcher.SendAsync(new DeleteReceiptCommand(id, CurrentUser), cancellationToken);

        return Ok(Done("Receipt deleted successfully"));
    }

    // POST: api/v1/Receipt/Post/1   (Draft -> Posted, updates PO received quantities and stock)
    [HttpPost("Post/{id:int}")]
    public async Task<IActionResult> Post(int id, CancellationToken cancellationToken)
    {
        await _dispatcher.SendAsync(new PostReceiptCommand(id, CurrentUser), cancellationToken);

        return Ok(Done("Receipt posted successfully"));
    }

    // POST: api/v1/Receipt/Cancel/1
    [HttpPost("Cancel/{id:int}")]
    public async Task<IActionResult> Cancel(int id, CancellationToken cancellationToken)
    {
        await _dispatcher.SendAsync(new CancelReceiptCommand(id, CurrentUser), cancellationToken);

        return Ok(Done("Receipt cancelled successfully"));
    }
}
