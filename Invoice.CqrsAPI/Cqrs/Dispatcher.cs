using System.Collections.Concurrent;

namespace Invoice.CqrsAPI.Cqrs;

/// <summary>
/// Finds the handler for a request, wraps it in the pipeline behaviors and runs it.
///
///   Controller -> Dispatcher -> [Logging] -> [Validation] -> Handler -> BAL service
/// </summary>
public sealed class Dispatcher : IDispatcher
{
    // One wrapper per request type, built once and cached.
    private static readonly ConcurrentDictionary<Type, object> Wrappers = new();

    private readonly IServiceProvider _serviceProvider;

    public Dispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task<TResponse> SendAsync<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var wrapper = (HandlerWrapper<TResponse>)Wrappers.GetOrAdd(
            request.GetType(),
            static (requestType, responseType) =>
                Activator.CreateInstance(
                    typeof(HandlerWrapper<,>).MakeGenericType(requestType, responseType))!,
            typeof(TResponse));

        return wrapper.HandleAsync(request, _serviceProvider, cancellationToken);
    }
}

internal abstract class HandlerWrapper<TResponse>
{
    public abstract Task<TResponse> HandleAsync(
        IRequest<TResponse> request,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken);
}

internal sealed class HandlerWrapper<TRequest, TResponse> : HandlerWrapper<TResponse>
    where TRequest : IRequest<TResponse>
{
    public override Task<TResponse> HandleAsync(
        IRequest<TResponse> request,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        var typedRequest = (TRequest)request;

        var handler = serviceProvider.GetRequiredService<IRequestHandler<TRequest, TResponse>>();
        var behaviors = serviceProvider.GetServices<IPipelineBehavior<TRequest, TResponse>>().ToArray();

        // Start with the real handler, then wrap it with each behavior from the inside out.
        // The first behavior registered ends up outermost (runs first).
        RequestHandlerDelegate<TResponse> pipeline =
            () => handler.HandleAsync(typedRequest, cancellationToken);

        for (var i = behaviors.Length - 1; i >= 0; i--)
        {
            var behavior = behaviors[i];
            var next = pipeline;
            pipeline = () => behavior.HandleAsync(typedRequest, next, cancellationToken);
        }

        return pipeline();
    }
}
