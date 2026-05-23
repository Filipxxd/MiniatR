using MiniatR;

namespace MiniatR.Tests.Fixtures;

public sealed class GetUserQueryHandler : IRequestHandler<GetUserQuery, UserResponse>
{
    public Task<UserResponse> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new UserResponse(request.Id, "Test User"));
    }
}

public sealed class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand>
{
    public static int CallCount { get; private set; }

    public Task Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        CallCount++;
        return Task.CompletedTask;
    }

    public static void Reset() => CallCount = 0;
}

public sealed class SlowQueryHandler : IRequestHandler<SlowQuery, string>
{
    public async Task<string> Handle(SlowQuery request, CancellationToken cancellationToken)
    {
        await Task.Delay(request.DelayMs, cancellationToken);
        return "completed";
    }
}

public sealed class NullableQueryHandler : IRequestHandler<NullableQuery, string?>
{
    public Task<string?> Handle(NullableQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult<string?>(null);
    }
}

public sealed class ThrowingQueryHandler : IRequestHandler<ThrowingQuery, string>
{
    public Task<string> Handle(ThrowingQuery request, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Handler threw an exception");
    }
}

public sealed class NestedQueryHandler : IRequestHandler<NestedQuery, int>
{
    private readonly ISender _sender;

    public NestedQueryHandler(ISender sender)
    {
        _sender = sender;
    }

    public async Task<int> Handle(NestedQuery request, CancellationToken cancellationToken)
    {
        if (request.Depth <= 0)
            return 0;

        var result = await _sender.Send(new NestedQuery(request.Depth - 1), cancellationToken);
        return result + 1;
    }
}

public sealed class CacheableQueryHandler : IRequestHandler<CacheableQuery, string>
{
    public Task<string> Handle(CacheableQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult($"value-for-{request.Key}");
    }
}

public sealed class NonCacheableQueryHandler : IRequestHandler<NonCacheableQuery, string>
{
    public Task<string> Handle(NonCacheableQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult($"value-for-{request.Key}");
    }
}

public sealed class HandlerWithDependencies : IRequestHandler<DependencyQuery, DependencyResponse>
{
    public ITestDependency Dependency { get; }

    public HandlerWithDependencies(ITestDependency dependency)
    {
        Dependency = dependency;
    }

    public Task<DependencyResponse> Handle(DependencyQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new DependencyResponse(request.Id, Dependency.GetValue()));
    }
}

public interface ITestDependency
{
    string GetValue();
}

public sealed class TestDependency : ITestDependency
{
    public string GetValue() => "From Dependency";
}
