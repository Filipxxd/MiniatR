using System.Collections.Concurrent;
using MiniatR;

namespace MiniatR.Tests.Fixtures;

public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public static ConcurrentQueue<string> Log { get; } = new();
    public static void Reset() => Log.Clear();

    public async Task<TResponse> Handle(TRequest request, PipelineDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        Log.Enqueue($"Before: {typeof(TRequest).Name}");
        var response = await next(cancellationToken);
        Log.Enqueue($"After: {typeof(TRequest).Name}");
        return response;
    }
}

public sealed class ShortCircuitBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public static bool ShouldShortCircuit { get; set; }
    public static TResponse? ShortCircuitResponse { get; set; }
    public static bool HandlerWasCalled { get; private set; }

    public static void Reset()
    {
        ShouldShortCircuit = false;
        ShortCircuitResponse = default;
        HandlerWasCalled = false;
    }

    public async Task<TResponse> Handle(TRequest request, PipelineDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (ShouldShortCircuit && ShortCircuitResponse is not null)
            return ShortCircuitResponse;

        HandlerWasCalled = true;
        return await next(cancellationToken);
    }
}

public sealed class ThrowingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public static bool ShouldThrow { get; set; }
    public static void Reset() => ShouldThrow = false;

    public Task<TResponse> Handle(TRequest request, PipelineDelegate<TResponse> next, CancellationToken cancellationToken)
        => ShouldThrow ? throw new InvalidOperationException("Behavior threw an exception") : next(cancellationToken);
}

public sealed class OrderTrackingBehavior<TRequest, TResponse>(int order) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public static ConcurrentQueue<string> ExecutionOrder { get; } = new();
    public static void Reset() => ExecutionOrder.Clear();

    public async Task<TResponse> Handle(TRequest request, PipelineDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        ExecutionOrder.Enqueue($"Behavior{order}-Before");
        var response = await next(cancellationToken);
        ExecutionOrder.Enqueue($"Behavior{order}-After");
        return response;
    }
}

public sealed class BehaviorWithDependencies<TRequest, TResponse>(ITestDependency dependency) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public Task<TResponse> Handle(TRequest request, PipelineDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _ = dependency.GetValue();
        return next(cancellationToken);
    }
}

public sealed class RetryBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public static int CallCount { get; private set; }
    public static int MaxRetries { get; set; } = 2;
    public static void Reset() { CallCount = 0; MaxRetries = 2; }

    public async Task<TResponse> Handle(TRequest request, PipelineDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        for (var i = 0; i <= MaxRetries; i++)
        {
            CallCount++;
            try
            {
                return await next(cancellationToken);
            }
            catch when (i < MaxRetries)
            {
            }
        }
        throw new InvalidOperationException("Should not reach here");
    }
}

public sealed class PassThroughBehavior<TRequest, TResponse>(int id) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public static ConcurrentQueue<int> ExecutionLog { get; } = new();
    public static void Reset() => ExecutionLog.Clear();

    public async Task<TResponse> Handle(TRequest request, PipelineDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        ExecutionLog.Enqueue(id);
        return await next(cancellationToken);
    }
}
