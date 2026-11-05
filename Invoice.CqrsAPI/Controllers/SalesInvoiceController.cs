using Asp.Versioning;
using Invoice.CqrsAPI.Cqrs;
using Invoice.CqrsAPI.Features.SalesInvoices;
using Invoice.CqrsAPI.Features.SalesInvoices.Commands;
using Invoice.CqrsAPI.Features.SalesInvoices.Queries;
using Invoice.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Invoice.CqrsAPI.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion(1.0)]
[ApiController]
[Authorize]
public class SalesInvoiceController : ApiControllerBase
{
    private readonly IDispatcher _dispatcher;

    public SalesInvoiceController(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    // ===================== QUERIES =====================

    // GET: api/v1/SalesInvoice/GetAll
    [HttpGet("GetAll")]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var data = await _dispatcher.SendAsync(new GetAllSalesInvoicesQuery(), cancellationToken);

        return Ok(Done("Sales invoices retrieved successfully", data));
    }

    // GET: api/v1/SalesInvoice/GetById/1
    [HttpGet("GetById/{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var data = await _dispatcher.SendAsync(new GetSalesInvoiceByIdQuery(id), cancellationToken);

        return Ok(Done("Sales invoice retrieved successfully", data));
    }

    // GET: api/v1/SalesInvoice/GetAllPaged?invoiceNumber=&customerId=&status=&pageNumber=1&pageSize=10
    [HttpGet("GetAllPaged")]
    public async Task<IActionResult> GetAllPaged(
        string? invoiceNumber,
        int? customerId,
        string? status,
        CancellationToken cancellationToken,
        int pageNumber = 1,
        int pageSize = 10)
    {
        var result = await _dispatcher.SendAsync(
            new GetSalesInvoicesPagedQuery(invoiceNumber, customerId, status, pageNumber, pageSize),
            cancellationToken);

        return Ok(Done("Sales invoices retrieved successfully", result));
    }

    // GET: api/v1/SalesInvoice/1/pdf
    [HttpGet("{id:int}/pdf")]
    [Produces("application/pdf")]
    public async Task<IActionResult> GetPdf(int id, CancellationToken cancellationToken)
    {
        var bytes = await _dispatcher.SendAsync(new GetSalesInvoicePdfQuery(id), cancellationToken);

        return File(bytes, "application/pdf", $"SalesInvoice-{id}.pdf");
    }

    // ===================== COMMANDS =====================

    // POST: api/v1/SalesInvoice/Create
    [HttpPost("Create")]
    public async Task<IActionResult> Create(
        [FromBody] SalesInvoiceDto dto,
        CancellationToken cancellationToken)
    {
        var id = await _dispatcher.SendAsync(
            new CreateSalesInvoiceCommand(SalesInvoiceData.From(dto), CurrentUser),
            cancellationToken);

        return Ok(Done("Sales invoice created successfully", id));
    }

    // PUT: api/v1/SalesInvoice/Update/1   (Draft only)
    [HttpPut("Update/{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] SalesInvoiceDto dto,
        CancellationToken cancellationToken)
    {
        await _dispatcher.SendAsync(
            new UpdateSalesInvoiceCommand(id, SalesInvoiceData.From(dto), CurrentUser),
            cancellationToken);

        return Ok(Done("Sales invoice updated successfully"));
    }

    // DELETE: api/v1/SalesInvoice/Delete/1
    [HttpDelete("Delete/{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _dispatcher.SendAsync(new DeleteSalesInvoiceCommand(id, CurrentUser), cancellationToken);

        return Ok(Done("Sales invoice deleted successfully"));
    }

    // POST: api/v1/SalesInvoice/Post/1   (Draft -> Posted, checks and reduces stock)
    [HttpPost("Post/{id:int}")]
    public async Task<IActionResult> Post(int id, CancellationToken cancellationToken)
    {
        await _dispatcher.SendAsync(new PostSalesInvoiceCommand(id, CurrentUser), cancellationToken);

        return Ok(Done("Sales invoice posted successfully"));
    }

    // POST: api/v1/SalesInvoice/Cancel/1
    [HttpPost("Cancel/{id:int}")]
    public async Task<IActionResult> Cancel(int id, CancellationToken cancellationToken)
    {
        await _dispatcher.SendAsync(new CancelSalesInvoiceCommand(id, CurrentUser), cancellationToken);

        return Ok(Done("Sales invoice cancelled successfully"));
    }
}
