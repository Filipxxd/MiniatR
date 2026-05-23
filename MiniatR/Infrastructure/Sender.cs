using Microsoft.Extensions.DependencyInjection;
using MiniatR.Exceptions;

namespace MiniatR;

internal sealed class Sender(IServiceProvider serviceProvider) : ISender
{
    private static readonly Type OpenHandlerType = typeof(IRequestHandler<,>);
    private static readonly Type OpenVoidHandlerType = typeof(IRequestHandler<>);
    private static readonly Type OpenBehaviorType = typeof(IPipelineBehavior<,>);

    private readonly IServiceProvider _serviceProvider = serviceProvider;

    public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var requestType = request.GetType();
        var responseType = typeof(TResponse);

        var handlerType = OpenHandlerType.MakeGenericType(requestType, responseType);
        var handler = _serviceProvider.GetService(handlerType)
            ?? throw new HandlerNotFoundException(requestType, responseType);

        var behaviorType = OpenBehaviorType.MakeGenericType(requestType, responseType);
        var behaviors = _serviceProvider.GetServices(behaviorType).Cast<object>().ToList();

        return await ExecutePipeline<TResponse>(requestType, responseType, request, handler, behaviors, cancellationToken).ConfigureAwait(false);
    }

    public async Task Send(IRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var requestType = request.GetType();

        var handlerType = OpenVoidHandlerType.MakeGenericType(requestType);
        var handler = _serviceProvider.GetService(handlerType)
            ?? throw new HandlerNotFoundException(requestType, null);

        var behaviorType = OpenBehaviorType.MakeGenericType(requestType, typeof(Nothing));
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

        for (var i = behaviors.Count - 1; i >= 0; i--)
        {
            var behavior = behaviors[i];
            var behaviorInvoker = InvokerCache.GetBehaviorInvoker<TResponse>(behavior.GetType(), requestType, responseType);
            var current = pipeline;

            pipeline = ct =>
            {
                ct.ThrowIfCancellationRequested();
                return behaviorInvoker(behavior, request, current, ct);
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

        PipelineDelegate<Nothing> pipeline = ct =>
        {
            ct.ThrowIfCancellationRequested();
            return InvokeVoidHandler(handlerInvoker, handler, request, ct);
        };

        for (var i = behaviors.Count - 1; i >= 0; i--)
        {
            var behavior = behaviors[i];
            var behaviorInvoker = InvokerCache.GetBehaviorInvoker<Nothing>(behavior.GetType(), requestType, typeof(Nothing));
            var current = pipeline;

            pipeline = ct =>
            {
                ct.ThrowIfCancellationRequested();
                return behaviorInvoker(behavior, request, current, ct);
            };
        }

        await pipeline(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<Nothing> InvokeVoidHandler(
        Func<object, object, CancellationToken, Task> invoker,
        object handler,
        object request,
        CancellationToken cancellationToken)
    {
        await invoker(handler, request, cancellationToken).ConfigureAwait(false);
        return Nothing.Value;
    }
}
