using Asp.Versioning;
using Invoice.CqrsAPI.Cqrs;
using Invoice.CqrsAPI.Features.Auth;
using Invoice.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Invoice.CqrsAPI.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
[ApiVersion(1.0)]
public class LoginController : ApiControllerBase
{
    private readonly IDispatcher _dispatcher;

    public LoginController(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _dispatcher.SendAsync(
            new LoginCommand(request.UserName, request.Password),
            cancellationToken);

        return Ok(Done("Login successful.", result));
    }
}
