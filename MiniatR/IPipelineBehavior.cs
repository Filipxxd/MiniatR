namespace MiniatR;

/// <summary>
/// Delegate representing the next step in the pipeline.
/// </summary>
/// <typeparam name="TResponse">The response type.</typeparam>
/// <param name="cancellationToken">Cancellation token.</param>
/// <returns>The response from the next step in the pipeline.</returns>
public delegate Task<TResponse> PipelineDelegate<TResponse>(CancellationToken cancellationToken);

/// <summary>
/// Defines a pipeline behavior that wraps request handling for cross-cutting concerns.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
/// <remarks>
/// The <paramref name="next"/> delegate can be called multiple times, enabling retry and caching patterns.
/// Each call will re-execute all inner behaviors and the handler.
/// </remarks>
public interface IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    /// <summary>
    /// Handles the request with access to the next delegate in the pipeline.
    /// </summary>
    /// <param name="request">The request being handled.</param>
    /// <param name="next">The next delegate in the pipeline. Can be called multiple times for retry/caching patterns.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The response.</returns>
    Task<TResponse> Handle(TRequest request, PipelineDelegate<TResponse> next, CancellationToken cancellationToken);
}
