using MiniatR;

namespace MiniatR.Tests.Fixtures;

public sealed class GetUserQueryHandler : IRequestHandler<GetUserQuery, UserResponse>
{
    public Task<UserResponse> Handle(GetUserQuery request, CancellationToken cancellationToken)
        => Task.FromResult(new UserResponse(request.Id, "Test User"));
}

public sealed class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand>
{
    public static int CallCount { get; private set; }
    public static void Reset() => CallCount = 0;

    public Task Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        CallCount++;
        return Task.CompletedTask;
    }
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
        => Task.FromResult<string?>(null);
}

public sealed class ThrowingQueryHandler : IRequestHandler<ThrowingQuery, string>
{
    public Task<string> Handle(ThrowingQuery request, CancellationToken cancellationToken)
        => throw new InvalidOperationException("Handler threw an exception");
}

public sealed class NestedQueryHandler(ISender sender) : IRequestHandler<NestedQuery, int>
{
    public async Task<int> Handle(NestedQuery request, CancellationToken cancellationToken)
    {
        if (request.Depth <= 0) return 0;
        var result = await sender.Send(new NestedQuery(request.Depth - 1), cancellationToken);
        return result + 1;
    }
}

public sealed class HandlerWithDependencies(ITestDependency dependency) : IRequestHandler<DependencyQuery, DependencyResponse>
{
    public Task<DependencyResponse> Handle(DependencyQuery request, CancellationToken cancellationToken)
        => Task.FromResult(new DependencyResponse(request.Id, dependency.GetValue()));
}

public interface ITestDependency { string GetValue(); }
public sealed class TestDependency : ITestDependency { public string GetValue() => "From Dependency"; }
