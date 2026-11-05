using Asp.Versioning;
using Invoice.CqrsAPI.Cqrs;
using Invoice.CqrsAPI.Features.Users;
using Invoice.CqrsAPI.Features.Users.Commands;
using Invoice.CqrsAPI.Features.Users.Queries;
using Invoice.DTOs;
using Invoice.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Invoice.CqrsAPI.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion(1.0)]
[ApiController]
[Authorize]
public class UserController : ApiControllerBase
{
    private readonly IDispatcher _dispatcher;

    public UserController(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    // ===================== QUERIES =====================

    // GET: api/v1/User/GetAll
    [HttpGet("GetAll")]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var data = await _dispatcher.SendAsync(new GetAllUsersQuery(), cancellationToken);

        return Ok(Done("Users retrieved successfully.", data));
    }

    // GET: api/v1/User/GetById/1
    [HttpGet("GetById/{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var user = await _dispatcher.SendAsync(new GetUserByIdQuery(id), cancellationToken);

        return Ok(Done("User retrieved successfully.", user));
    }

    // GET: api/v1/User/GetAllPaged?UserName=&Email=&FirstName=&LastName=&IsActive=&PageNumber=1&PageSize=10
    [HttpGet("GetAllPaged")]
    public async Task<IActionResult> GetAllPaged(
        [FromQuery] UserFilterDto filter,
        CancellationToken cancellationToken)
    {
        var result = await _dispatcher.SendAsync(
            new GetUsersPagedQuery(
                filter.UserName, filter.Email, filter.FirstName, filter.LastName,
                filter.IsActive, filter.PageNumber, filter.PageSize),
            cancellationToken);

        return Ok(new ApiResponse<PagedResultDto<UserDto>>
        {
            Success = true,
            Message = "Users retrieved successfully.",
            Data = result,
            TotalRecords = result.TotalRecords
        });
    }

    // POST: api/v1/User/ValidateUser
    [AllowAnonymous]
    [HttpPost("ValidateUser")]
    public async Task<IActionResult> ValidateUser(
        [FromBody] UserLoginRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _dispatcher.SendAsync(
            new ValidateUserQuery(request.UserName, request.Password),
            cancellationToken);

        return Ok(Done("User validated successfully.", user));
    }

    // ===================== COMMANDS =====================

    // POST: api/v1/User/Create   (anonymous, same as Invoice.CoreAPI: used for sign-up)
    [AllowAnonymous]
    [HttpPost("Create")]
    public async Task<IActionResult> Create(
        [FromBody] UserCreateDto dto,
        CancellationToken cancellationToken)
    {
        var user = await _dispatcher.SendAsync(
            new CreateUserCommand(UserProfileData.From(dto), dto.Password, CurrentUser),
            cancellationToken);

        return Ok(Done("User created successfully.", user));
    }

    // PUT: api/v1/User/Update/1
    [HttpPut("Update/{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UserUpdateDto dto,
        CancellationToken cancellationToken)
    {
        var user = await _dispatcher.SendAsync(
            new UpdateUserCommand(id, UserProfileData.From(dto), dto.Password, CurrentUser),
            cancellationToken);

        return Ok(Done("User updated successfully.", user));
    }

    // DELETE: api/v1/User/Delete/1
    [HttpDelete("Delete/{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _dispatcher.SendAsync(new DeleteUserCommand(id, CurrentUserId), cancellationToken);

        return Ok(Done("User deleted successfully."));
    }
}
