using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace MiniatR;

internal static class InvokerCache
{
    private static readonly ConcurrentDictionary<(Type RequestType, Type ResponseType), Delegate> _handlerInvokers = new();
    private static readonly ConcurrentDictionary<(Type BehaviorType, Type ResponseType), Delegate> _behaviorInvokers = new();

    internal static Func<object, object, CancellationToken, Task<TResponse>> GetHandlerInvoker<TResponse>(
        Type requestType, Type responseType)
    {
        return (Func<object, object, CancellationToken, Task<TResponse>>)
            _handlerInvokers.GetOrAdd((requestType, responseType), key => CompileHandlerInvoker<TResponse>(key.Item1, key.Item2));
    }

    internal static Func<object, object, CancellationToken, Task> GetVoidHandlerInvoker(Type requestType)
    {
        return (Func<object, object, CancellationToken, Task>)
            _handlerInvokers.GetOrAdd((requestType, typeof(Nothing)), key => CompileVoidHandlerInvoker(key.Item1));
    }

    internal static Func<object, object, PipelineDelegate<TResponse>, CancellationToken, Task<TResponse>> GetBehaviorInvoker<TResponse>(
        Type behaviorType, Type requestType, Type responseType)
    {
        return (Func<object, object, PipelineDelegate<TResponse>, CancellationToken, Task<TResponse>>)
            _behaviorInvokers.GetOrAdd((behaviorType, responseType), _ => CompileBehaviorInvoker<TResponse>(behaviorType, requestType, responseType));
    }

    private static Func<object, object, CancellationToken, Task<TResponse>> CompileHandlerInvoker<TResponse>(
        Type requestType, Type responseType)
    {
        var handlerInterfaceType = typeof(IRequestHandler<,>).MakeGenericType(requestType, responseType);
        var handleMethod = handlerInterfaceType.GetMethod("Handle")
            ?? throw new InvalidOperationException(
                $"Method 'Handle' not found on '{handlerInterfaceType.FullName}'.");

        var handlerParam = Expression.Parameter(typeof(object), "handler");
        var requestParam = Expression.Parameter(typeof(object), "request");
        var ctParam = Expression.Parameter(typeof(CancellationToken), "ct");

        var call = Expression.Call(
            Expression.Convert(handlerParam, handlerInterfaceType),
            handleMethod,
            Expression.Convert(requestParam, requestType),
            ctParam);

        return Expression.Lambda<Func<object, object, CancellationToken, Task<TResponse>>>(
            call, handlerParam, requestParam, ctParam).Compile();
    }

    private static Func<object, object, CancellationToken, Task> CompileVoidHandlerInvoker(Type requestType)
    {
        var handlerInterfaceType = typeof(IRequestHandler<>).MakeGenericType(requestType);
        var handleMethod = handlerInterfaceType.GetMethod("Handle")
            ?? throw new InvalidOperationException(
                $"Method 'Handle' not found on '{handlerInterfaceType.FullName}'.");

        var handlerParam = Expression.Parameter(typeof(object), "handler");
        var requestParam = Expression.Parameter(typeof(object), "request");
        var ctParam = Expression.Parameter(typeof(CancellationToken), "ct");

        var call = Expression.Call(
            Expression.Convert(handlerParam, handlerInterfaceType),
            handleMethod,
            Expression.Convert(requestParam, requestType),
            ctParam);

        return Expression.Lambda<Func<object, object, CancellationToken, Task>>(
            call, handlerParam, requestParam, ctParam).Compile();
    }

    private static Func<object, object, PipelineDelegate<TResponse>, CancellationToken, Task<TResponse>> CompileBehaviorInvoker<TResponse>(
        Type behaviorType, Type requestType, Type responseType)
    {
        var behaviorInterfaceType = typeof(IPipelineBehavior<,>).MakeGenericType(requestType, responseType);
        var handleMethod = behaviorInterfaceType.GetMethod("Handle")
            ?? throw new InvalidOperationException(
                $"Method 'Handle' not found on '{behaviorInterfaceType.FullName}'.");

        var behaviorParam = Expression.Parameter(typeof(object), "behavior");
        var requestParam = Expression.Parameter(typeof(object), "request");
        var nextParam = Expression.Parameter(typeof(PipelineDelegate<TResponse>), "next");
        var ctParam = Expression.Parameter(typeof(CancellationToken), "ct");

        var call = Expression.Call(
            Expression.Convert(behaviorParam, behaviorInterfaceType),
            handleMethod,
            Expression.Convert(requestParam, requestType),
            nextParam,
            ctParam);

        return Expression.Lambda<Func<object, object, PipelineDelegate<TResponse>, CancellationToken, Task<TResponse>>>(
            call, behaviorParam, requestParam, nextParam, ctParam).Compile();
    }
}
