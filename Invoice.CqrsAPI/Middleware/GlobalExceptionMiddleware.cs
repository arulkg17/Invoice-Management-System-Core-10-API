using System.Text.Json;
using Invoice.CqrsAPI.Common;
using Invoice.CqrsAPI.Cqrs;
using Invoice.Model;

namespace Invoice.CqrsAPI.Middleware;

/// <summary>
/// ONE place that turns exceptions into HTTP responses, so handlers just throw and
/// controllers have no try/catch (compare with Invoice.CoreAPI's CategoryController).
///
///   RequestValidationException  -> 400 (with the list of failures)
///   BusinessRuleException       -> 400
///   NotFoundException           -> 404
///   ConflictException           -> 409
///   InvalidCredentialsException -> 401
///   anything else               -> 500 (logged with stack trace)
/// </summary>
public sealed class GlobalExceptionMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // Client closed the connection - nothing to send back.
            context.Response.StatusCode = 499;
        }
        catch (Exception ex)
        {
            await WriteErrorAsync(context, ex);
        }
    }

    private async Task WriteErrorAsync(HttpContext context, Exception ex)
    {
        if (context.Response.HasStarted)
        {
            _logger.LogError(ex, "Exception after the response had started. {Method} {Path}",
                context.Request.Method, context.Request.Path);
            return;
        }

        int status;
        string message;
        string details;
        object? data = null;

        switch (ex)
        {
            case RequestValidationException validation:
                status = StatusCodes.Status400BadRequest;
                message = "Validation failed.";
                details = string.Join("; ", validation.Failures.Select(f => $"{f.Property}: {f.Message}"));
                data = validation.Failures;
                _logger.LogWarning("Validation failed. {Method} {Path}: {Details}",
                    context.Request.Method, context.Request.Path, details);
                break;

            case BusinessRuleException business:
                status = StatusCodes.Status400BadRequest;
                message = business.Message;
                details = business.Message;
                break;

            case NotFoundException notFound:
                status = StatusCodes.Status404NotFound;
                message = notFound.Message;
                details = notFound.Message;
                break;

            case ConflictException conflict:
                status = StatusCodes.Status409Conflict;
                message = conflict.Message;
                details = conflict.Message;
                break;

            case InvalidCredentialsException invalid:
                status = StatusCodes.Status401Unauthorized;
                message = invalid.Message;
                details = invalid.Message;
                break;

            default:
                status = StatusCodes.Status500InternalServerError;
                message = "An unexpected error occurred.";
                details = ex.Message;
                _logger.LogError(ex, "Unhandled exception. {Method} {Path}",
                    context.Request.Method, context.Request.Path);
                break;
        }

        var body = new ApiResponse<object>
        {
            Success = false,
            Message = message,
            Data = data,
            Error = new ApiError { Code = status.ToString(), Details = details }
        };

        context.Response.Clear();
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(body, JsonOptions);
    }
}

public static class GlobalExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<GlobalExceptionMiddleware>();
    }
}
