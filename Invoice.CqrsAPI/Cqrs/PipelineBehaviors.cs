using System.Diagnostics;
using Invoice.Model;

namespace Invoice.CqrsAPI.Cqrs;

/// <summary>Logs every command / query: name, duration and outcome.</summary>
public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var name = typeof(TRequest).Name;
        var timer = Stopwatch.StartNew();

        _logger.LogInformation("Handling {Request}", name);

        try
        {
            var response = await next();

            _logger.LogInformation("Handled {Request} in {ElapsedMs} ms", name, timer.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex) when (
            ex is NotFoundException or BusinessRuleException or RequestValidationException)
        {
            // Expected, caller-caused problems: warning, no stack trace.
            _logger.LogWarning("{Request} rejected in {ElapsedMs} ms: {Reason}",
                name, timer.ElapsedMilliseconds, ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Request} failed in {ElapsedMs} ms", name, timer.ElapsedMilliseconds);
            throw;
        }
    }
}

/// <summary>Runs every IValidator registered for the request BEFORE the handler runs.</summary>
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var failures = _validators
            .SelectMany(v => v.Validate(request))
            .ToList();

        if (failures.Count > 0)
        {
            throw new RequestValidationException(failures);
        }

        return next();
    }
}
