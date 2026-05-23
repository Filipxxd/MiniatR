using Microsoft.Extensions.DependencyInjection;
using MiniatR.Exceptions;

namespace MiniatR;

internal sealed class Sender(IServiceProvider serviceProvider) : ISender
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var requestType = request.GetType();
        var responseType = typeof(TResponse);

        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, responseType);
        var handler = _serviceProvider.GetService(handlerType)
            ?? throw new HandlerNotFoundException(requestType, responseType);

        var behaviorType = typeof(IPipelineBehavior<,>).MakeGenericType(requestType, responseType);
        var behaviors = _serviceProvider.GetServices(behaviorType).Cast<object>().ToList();

        return await ExecutePipeline<TResponse>(requestType, responseType, request, handler, behaviors, cancellationToken).ConfigureAwait(false);
    }

    public async Task Send(IRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var requestType = request.GetType();

        var handlerType = typeof(IRequestHandler<>).MakeGenericType(requestType);
        var handler = _serviceProvider.GetService(handlerType)
            ?? throw new HandlerNotFoundException(requestType, null);

        var behaviorType = typeof(IPipelineBehavior<,>).MakeGenericType(requestType, typeof(Nothing));
        var behaviors = _serviceProvider.GetServices(behaviorType).Cast<object>().ToList();

        await ExecuteVoidPipeline(requestType, request, handler, behaviors, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<TResponse> ExecutePipeline<TResponse>(
        Type requestType,
        Type responseType,
        object request,
        object handler,
        List<object> behaviors,
        CancellationToken cancellationToken)
    {
        var handlerInvoker = InvokerCache.GetHandlerInvoker<TResponse>(requestType, responseType);

        PipelineDelegate<TResponse> pipeline = ct =>
        {
            ct.ThrowIfCancellationRequested();
            return handlerInvoker(handler, request, ct);
        };

        foreach (var behavior in Enumerable.Reverse(behaviors))
        {
            var behaviorInvoker = InvokerCache.GetBehaviorInvoker<TResponse>(behavior.GetType(), requestType, responseType);
            var current = pipeline;
            var captured = behavior;

            pipeline = ct =>
            {
                ct.ThrowIfCancellationRequested();
                return behaviorInvoker(captured, request, current, ct);
            };
        }

        return await pipeline(cancellationToken).ConfigureAwait(false);
    }

    private static async Task ExecuteVoidPipeline(
        Type requestType,
        object request,
        object handler,
        List<object> behaviors,
        CancellationToken cancellationToken)
    {
        var handlerInvoker = InvokerCache.GetVoidHandlerInvoker(requestType);

        PipelineDelegate<Nothing> pipeline = async ct =>
        {
            ct.ThrowIfCancellationRequested();
            await handlerInvoker(handler, request, ct).ConfigureAwait(false);
            return Nothing.Value;
        };

        foreach (var behavior in Enumerable.Reverse(behaviors))
        {
            var behaviorInvoker = InvokerCache.GetBehaviorInvoker<Nothing>(behavior.GetType(), requestType, typeof(Nothing));
            var current = pipeline;
            var captured = behavior;

            pipeline = ct =>
            {
                ct.ThrowIfCancellationRequested();
                return behaviorInvoker(captured, request, current, ct);
            };
        }

        await pipeline(cancellationToken).ConfigureAwait(false);
    }
}
