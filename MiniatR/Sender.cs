using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.DependencyInjection;
using MiniatR.Exceptions;

namespace MiniatR;

internal sealed class Sender(IServiceProvider serviceProvider) : ISender
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
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

        return ExecutePipeline<TResponse>(request, handler, behaviors, cancellationToken);
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

        await ExecuteVoidPipeline(request, handler, behaviors, cancellationToken);
    }

    private static async Task<TResponse> ExecutePipeline<TResponse>(
        object request,
        object handler,
        List<object> behaviors,
        CancellationToken cancellationToken)
    {
        PipelineDelegate<TResponse> handlerDelegate = ct =>
        {
            ct.ThrowIfCancellationRequested();
            return InvokeHandler<TResponse>(handler, request, ct);
        };

        foreach (var behavior in Enumerable.Reverse(behaviors))
        {
            var current = handlerDelegate;
            handlerDelegate = ct =>
            {
                ct.ThrowIfCancellationRequested();
                return InvokeBehavior<TResponse>(behavior, request, current, ct);
            };
        }

        return await handlerDelegate(cancellationToken);
    }

    private static Task<TResponse> InvokeHandler<TResponse>(object handler, object request, CancellationToken cancellationToken)
    {
        try
        {
            var method = handler.GetType().GetMethod("Handle")!;
            return (Task<TResponse>)method.Invoke(handler, [request, cancellationToken])!;
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;
        }
    }

    private static Task<TResponse> InvokeBehavior<TResponse>(
        object behavior,
        object request,
        PipelineDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        try
        {
            var method = behavior.GetType().GetMethod("Handle")!;
            return (Task<TResponse>)method.Invoke(behavior, [request, next, cancellationToken])!;
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;
        }
    }

    private static async Task ExecuteVoidPipeline(
        object request,
        object handler,
        List<object> behaviors,
        CancellationToken cancellationToken)
    {
        PipelineDelegate<Nothing> handlerDelegate = async ct =>
        {
            ct.ThrowIfCancellationRequested();
            await InvokeVoidHandler(handler, request, ct);
            return Nothing.Value;
        };

        foreach (var behavior in Enumerable.Reverse(behaviors))
        {
            var current = handlerDelegate;
            handlerDelegate = ct =>
            {
                ct.ThrowIfCancellationRequested();
                return InvokeBehavior(behavior, request, current, ct);
            };
        }

        await handlerDelegate(cancellationToken);
    }

    private static async Task InvokeVoidHandler(object handler, object request, CancellationToken cancellationToken)
    {
        try
        {
            var method = handler.GetType().GetMethod("Handle")!;
            await (Task)method.Invoke(handler, [request, cancellationToken])!;
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
        }
    }
}
