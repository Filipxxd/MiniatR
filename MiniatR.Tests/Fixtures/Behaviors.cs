using MiniatR;

namespace MiniatR.Tests.Fixtures;

public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public static List<string> Log { get; } = [];
    public static void Reset() => Log.Clear();

    public async Task<TResponse> Handle(TRequest request, PipelineDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        Log.Add($"Before: {typeof(TRequest).Name}");
        var response = await next(cancellationToken);
        Log.Add($"After: {typeof(TRequest).Name}");
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
    public static List<string> ExecutionOrder { get; } = [];
    public static void Reset() => ExecutionOrder.Clear();

    public async Task<TResponse> Handle(TRequest request, PipelineDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        ExecutionOrder.Add($"Behavior{order}-Before");
        var response = await next(cancellationToken);
        ExecutionOrder.Add($"Behavior{order}-After");
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
