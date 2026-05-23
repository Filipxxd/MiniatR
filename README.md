# MiniatR

Lightweight mediator for .NET — just requests, handlers, and pipeline behaviors. Nothing more.

## Installation

```bash
dotnet add package MiniatR
```

## Quick Start

```csharp
using MiniatR;
using MiniatR.Extensions;

// Define request and handler
public sealed record GetUserQuery(Guid Id) : IRequest<UserResponse>;
public sealed record UserResponse(Guid Id, string Name);

public sealed class GetUserQueryHandler : IRequestHandler<GetUserQuery, UserResponse>
{
    public Task<UserResponse> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new UserResponse(request.Id, "John Doe"));
    }
}

// Register
services.AddMiniatR(cfg => cfg.RegisterServicesFromAssemblyContaining<GetUserQuery>());

// Use
public class MyService(ISender sender)
{
    public Task<UserResponse> GetUser(Guid id) => sender.Send(new GetUserQuery(id));
}
```

## Void Requests

```csharp
public sealed record DeleteUserCommand(Guid Id) : IRequest;

public sealed class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand>
{
    public Task Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        // No return needed
        return Task.CompletedTask;
    }
}

await sender.Send(new DeleteUserCommand(userId));
```

## Cancellation Support

All requests support cancellation tokens. The pipeline checks for cancellation at each step.

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
var result = await sender.Send(new SlowQuery(), cts.Token);
```

If cancelled, throws `OperationCanceledException` before execution or `TaskCanceledException` during.

## Pipeline Behaviors

```csharp
public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, PipelineDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Handling {typeof(TRequest).Name}");
        var response = await next(cancellationToken);
        Console.WriteLine($"Handled {typeof(TRequest).Name}");
        return response;
    }
}

// Register globally
services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

// Or for specific request types
services.AddMiniatR(cfg => cfg
    .RegisterServicesFromAssemblyContaining<GetUserQuery>()
    .AddBehavior<GetUserQuery, UserResponse, SpecificBehavior>());
```

## Configuration

```csharp
services.AddMiniatR(cfg => cfg
    .RegisterServicesFromAssemblyContaining<GetUserQuery>()
    .RegisterServicesFromAssembly(typeof(OtherHandler).Assembly)
    .WithHandlerLifetime(ServiceLifetime.Transient)
    .WithBehaviorLifetime(ServiceLifetime.Scoped));
```

## License

MIT
