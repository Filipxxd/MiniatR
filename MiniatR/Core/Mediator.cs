using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.DependencyInjection;
using MiniatR.Abstractions;
using MiniatR.Abstractions.Exceptions;

namespace MiniatR.Core;

internal sealed class Mediator(IServiceProvider serviceProvider) : IMediator
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

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
        RequestHandlerDelegate<TResponse> handlerDelegate = () =>
            InvokeHandler<TResponse>(handler, request, cancellationToken);

        foreach (var behavior in Enumerable.Reverse(behaviors))
        {
            var current = handlerDelegate;
            handlerDelegate = () => InvokeBehavior<TResponse>(behavior, request, current, cancellationToken);
        }

        return await handlerDelegate();
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
        RequestHandlerDelegate<TResponse> next,
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
        RequestHandlerDelegate<Nothing> handlerDelegate = async () =>
        {
            await InvokeVoidHandler(handler, request, cancellationToken);
            return Nothing.Value;
        };

        foreach (var behavior in Enumerable.Reverse(behaviors))
        {
            var current = handlerDelegate;
            handlerDelegate = () => InvokeBehavior(behavior, request, current, cancellationToken);
        }

        await handlerDelegate();
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
