using MiniatR.Exceptions;

namespace MiniatR;

/// <summary>
/// Sends requests through the mediator pipeline to their corresponding handlers.
/// </summary>
public interface ISender
{
    /// <summary>
    /// Sends a request through the pipeline and returns the response.
    /// </summary>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="request">The request to send.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The response from the handler.</returns>
    /// <exception cref="HandlerNotFoundException">Thrown when no handler is registered for the request type.</exception>
    /// <remarks>
    /// Cancellation is checked before pipeline execution begins, before each pipeline behavior executes,
    /// and before the handler executes. Handlers must check the token themselves for long-running operations.
    /// </remarks>
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a void request through the pipeline.
    /// </summary>
    /// <param name="request">The request to send.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <exception cref="HandlerNotFoundException">Thrown when no handler is registered for the request type.</exception>
    /// <remarks>
    /// Cancellation is checked before pipeline execution begins, before each pipeline behavior executes,
    /// and before the handler executes. Handlers must check the token themselves for long-running operations.
    /// </remarks>
    Task Send(IRequest request, CancellationToken cancellationToken = default);
}
