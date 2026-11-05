using Asp.Versioning;
using Invoice.CqrsAPI.Cqrs;
using Invoice.CqrsAPI.Features.Customers;
using Invoice.CqrsAPI.Features.Customers.Commands;
using Invoice.CqrsAPI.Features.Customers.Queries;
using Invoice.DTOs;
using Invoice.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Invoice.CqrsAPI.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion(1.0)]
[ApiController]
[Authorize]
public class CustomerController : ApiControllerBase
{
    private readonly IDispatcher _dispatcher;

    public CustomerController(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    // ===================== QUERIES =====================

    // GET: api/v1/Customer/GetAll
    [HttpGet("GetAll")]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var data = await _dispatcher.SendAsync(new GetAllCustomersQuery(), cancellationToken);

        return Ok(Done("Customers retrieved successfully.", data));
    }

    // GET: api/v1/Customer/GetById/5
    [HttpGet("GetById/{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var item = await _dispatcher.SendAsync(new GetCustomerByIdQuery(id), cancellationToken);

        return Ok(Done("Customer retrieved successfully.", item));
    }

    // GET: api/v1/Customer/GetAllPaged?CustomerCode=&CustomerName=&MobileNo=&City=&pageNumber=1&pageSize=10
    [HttpGet("GetAllPaged")]
    public async Task<IActionResult> GetAllPaged(
        string? CustomerCode,
        string? CustomerName,
        string? MobileNo,
        string? City,
        CancellationToken cancellationToken,
        int pageNumber = 1,
        int pageSize = 10)
    {
        var result = await _dispatcher.SendAsync(
            new GetCustomersPagedQuery(CustomerCode, CustomerName, MobileNo, City, pageNumber, pageSize),
            cancellationToken);

        return Ok(new ApiResponse<PagedResultDto<CustomerDto>>
        {
            Success = true,
            Message = "Customers retrieved successfully.",
            Data = result,
            TotalRecords = result.TotalRecords
        });
    }

    // ===================== COMMANDS =====================

    // POST: api/v1/Customer/Create
    [HttpPost("Create")]
    public async Task<IActionResult> Create(
        [FromBody] CustomerDto dto,
        CancellationToken cancellationToken)
    {
        var id = await _dispatcher.SendAsync(
            new CreateCustomerCommand(CustomerData.From(dto), CurrentUser),
            cancellationToken);

        return Ok(Done("Customer created successfully.", id));
    }

    // PUT: api/v1/Customer/Update/5   (the Id in the URL is the one that counts)
    [HttpPut("Update/{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] CustomerDto dto,
        CancellationToken cancellationToken)
    {
        await _dispatcher.SendAsync(
            new UpdateCustomerCommand(id, CustomerData.From(dto), CurrentUser),
            cancellationToken);

        return Ok(Done("Customer updated successfully."));
    }

    // DELETE: api/v1/Customer/Delete/5
    [HttpDelete("Delete/{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _dispatcher.SendAsync(new DeleteCustomerCommand(id), cancellationToken);

        return Ok(Done("Customer deleted successfully."));
    }
}
