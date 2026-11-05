using Asp.Versioning;
using Invoice.CqrsAPI.Cqrs;
using Invoice.CqrsAPI.Features.Vendors;
using Invoice.CqrsAPI.Features.Vendors.Commands;
using Invoice.CqrsAPI.Features.Vendors.Queries;
using Invoice.DTOs;
using Invoice.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Invoice.CqrsAPI.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion(1.0)]
[ApiController]
[Authorize]
public class VendorController : ApiControllerBase
{
    private readonly IDispatcher _dispatcher;

    public VendorController(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    // ===================== QUERIES =====================

    // GET: api/v1/Vendor/GetAll
    [HttpGet("GetAll")]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var data = await _dispatcher.SendAsync(new GetAllVendorsQuery(), cancellationToken);

        return Ok(Done("Vendors retrieved successfully.", data));
    }

    // GET: api/v1/Vendor/GetById/5
    [HttpGet("GetById/{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var item = await _dispatcher.SendAsync(new GetVendorByIdQuery(id), cancellationToken);

        return Ok(Done("Vendor retrieved successfully.", item));
    }

    // GET: api/v1/Vendor/GetAllPaged?VendorCode=&VendorName=&MobileNo=&City=&pageNumber=1&pageSize=10
    [HttpGet("GetAllPaged")]
    public async Task<IActionResult> GetAllPaged(
        string? VendorCode,
        string? VendorName,
        string? MobileNo,
        string? City,
        CancellationToken cancellationToken,
        int pageNumber = 1,
        int pageSize = 10)
    {
        var result = await _dispatcher.SendAsync(
            new GetVendorsPagedQuery(VendorCode, VendorName, MobileNo, City, pageNumber, pageSize),
            cancellationToken);

        return Ok(new ApiResponse<PagedResultDto<VendorDto>>
        {
            Success = true,
            Message = "Vendors retrieved successfully.",
            Data = result,
            TotalRecords = result.TotalRecords
        });
    }

    // ===================== COMMANDS =====================

    // POST: api/v1/Vendor/Create
    [HttpPost("Create")]
    public async Task<IActionResult> Create(
        [FromBody] VendorDto dto,
        CancellationToken cancellationToken)
    {
        var id = await _dispatcher.SendAsync(
            new CreateVendorCommand(VendorData.From(dto), CurrentUser),
            cancellationToken);

        return Ok(Done("Vendor created successfully.", id));
    }

    // PUT: api/v1/Vendor/Update/5   (the Id in the URL is the one that counts)
    [HttpPut("Update/{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] VendorDto dto,
        CancellationToken cancellationToken)
    {
        await _dispatcher.SendAsync(
            new UpdateVendorCommand(id, VendorData.From(dto), CurrentUser),
            cancellationToken);

        return Ok(Done("Vendor updated successfully."));
    }

    // DELETE: api/v1/Vendor/Delete/5
    [HttpDelete("Delete/{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _dispatcher.SendAsync(new DeleteVendorCommand(id), cancellationToken);

        return Ok(Done("Vendor deleted successfully."));
    }
}
