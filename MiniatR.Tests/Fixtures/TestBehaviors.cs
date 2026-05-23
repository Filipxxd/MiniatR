using MiniatR.Abstractions;

namespace MiniatR.Tests.Fixtures;

public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public static List<string> Log { get; } = [];

    public async Task<TResponse> Handle(TRequest request, PipelineDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        Log.Add($"Before: {typeof(TRequest).Name}");
        var response = await next();
        Log.Add($"After: {typeof(TRequest).Name}");
        return response;
    }

    public static void Reset() => Log.Clear();
}

public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public static List<string> Log { get; } = [];

    public async Task<TResponse> Handle(TRequest request, PipelineDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        Log.Add($"Validating: {typeof(TRequest).Name}");
        var response = await next();
        Log.Add($"Validated: {typeof(TRequest).Name}");
        return response;
    }

    public static void Reset() => Log.Clear();
}

public sealed class ShortCircuitBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public static bool ShouldShortCircuit { get; set; }
    public static TResponse? ShortCircuitResponse { get; set; }
    public static bool HandlerWasCalled { get; private set; }

    public async Task<TResponse> Handle(TRequest request, PipelineDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (ShouldShortCircuit && ShortCircuitResponse is not null)
            return ShortCircuitResponse;

        HandlerWasCalled = true;
        return await next();
    }

    public static void Reset()
    {
        ShouldShortCircuit = false;
        ShortCircuitResponse = default;
        HandlerWasCalled = false;
    }
}

public sealed class ThrowingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public static bool ShouldThrow { get; set; }

    public Task<TResponse> Handle(TRequest request, PipelineDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (ShouldThrow)
            throw new InvalidOperationException("Behavior threw an exception");

        return next();
    }

    public static void Reset() => ShouldThrow = false;
}

public sealed class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICacheableQuery
{
    public static List<string> Log { get; } = [];

    public async Task<TResponse> Handle(TRequest request, PipelineDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        Log.Add($"Cache check: {typeof(TRequest).Name}");
        var response = await next();
        Log.Add($"Cache store: {typeof(TRequest).Name}");
        return response;
    }

    public static void Reset() => Log.Clear();
}

public sealed class GetUserQueryLoggingBehavior : IPipelineBehavior<GetUserQuery, UserResponse>
{
    public static List<string> Log { get; } = [];

    public async Task<UserResponse> Handle(GetUserQuery request, PipelineDelegate<UserResponse> next, CancellationToken cancellationToken)
    {
        Log.Add($"GetUserQuery specific behavior: {request.Id}");
        var response = await next();
        Log.Add($"GetUserQuery response: {response.Name}");
        return response;
    }

    public static void Reset() => Log.Clear();
}

public sealed class BehaviorWithDependencies<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public ITestDependency Dependency { get; }

    public BehaviorWithDependencies(ITestDependency dependency)
    {
        Dependency = dependency;
    }

    public Task<TResponse> Handle(TRequest request, PipelineDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _ = Dependency.GetValue();
        return next();
    }
}

public sealed class OrderTrackingBehavior1<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public static List<string> ExecutionOrder { get; } = [];

    public async Task<TResponse> Handle(TRequest request, PipelineDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        ExecutionOrder.Add("Behavior1-Before");
        var response = await next();
        ExecutionOrder.Add("Behavior1-After");
        return response;
    }

    public static void Reset() => ExecutionOrder.Clear();
}

public sealed class OrderTrackingBehavior2<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public static List<string> ExecutionOrder => OrderTrackingBehavior1<TRequest, TResponse>.ExecutionOrder;

    public async Task<TResponse> Handle(TRequest request, PipelineDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        ExecutionOrder.Add("Behavior2-Before");
        var response = await next();
        ExecutionOrder.Add("Behavior2-After");
        return response;
    }
}

public sealed class OrderTrackingBehavior3<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public static List<string> ExecutionOrder => OrderTrackingBehavior1<TRequest, TResponse>.ExecutionOrder;

    public async Task<TResponse> Handle(TRequest request, PipelineDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        ExecutionOrder.Add("Behavior3-Before");
        var response = await next();
        ExecutionOrder.Add("Behavior3-After");
        return response;
    }
}
