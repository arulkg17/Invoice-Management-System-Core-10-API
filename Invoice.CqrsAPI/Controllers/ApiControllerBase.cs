using System.Security.Claims;
using Invoice.Model;
using Microsoft.AspNetCore.Mvc;

namespace Invoice.CqrsAPI.Controllers;

/// <summary>
/// Helpers shared by every controller. Note what is NOT here: no try/catch.
/// Errors are handled once, in GlobalExceptionMiddleware.
/// </summary>
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>User name from the JWT, used for CreatedBy / UpdatedBy.</summary>
    protected string CurrentUser =>
        User?.Identity?.Name
        ?? User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? "system";

    /// <summary>Numeric user id from the JWT (NameIdentifier), or null when not logged in.</summary>
    protected int? CurrentUserId =>
        int.TryParse(User?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;

    protected static ApiResponse<string> Done(string message)
    {
        return new ApiResponse<string> { Success = true, Message = message };
    }

    protected static ApiResponse<T> Done<T>(string message, T data)
    {
        return new ApiResponse<T> { Success = true, Message = message, Data = data };
    }
}
