namespace Invoice.CqrsAPI.Cqrs;

// =====================================================================
//  The whole "mediator" idea in one file.
//
//  Command  = "do something"  (changes data)          -> ICommand<TResult>
//  Query    = "give me data"  (never changes data)    -> IQuery<TResult>
//  Handler  = exactly ONE handler per command / query
//  Behavior = cross-cutting step wrapped around every handler (logging, validation ...)
//
//  We wrote this ourselves (about 100 lines) instead of using MediatR so students can
//  read every line of it. MediatR works the same way, it is just bigger.
// =====================================================================

/// <summary>Return type for commands that have nothing to return (Update, Delete).</summary>
public readonly record struct Unit
{
    public static readonly Unit Value = new();
}

public interface IRequest<out TResponse> { }

/// <summary>A message that changes state.</summary>
public interface ICommand<out TResponse> : IRequest<TResponse> { }

/// <summary>A message that only reads state.</summary>
public interface IQuery<out TResponse> : IRequest<TResponse> { }

public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken);
}

public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
}

public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
}

/// <summary>"Call the next step" - either the next behavior or the real handler.</summary>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();

public interface IPipelineBehavior<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken);
}

/// <summary>What controllers talk to. They never see handlers or services.</summary>
public interface IDispatcher
{
    Task<TResponse> SendAsync<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default);
}
