# MiniatR

A lightweight mediator library for .NET. Simple request/response dispatching with pipeline behaviors.

## Features

- **Minimal API** — Just `IRequest`, `IRequestHandler`, and `ISender`
- **Pipeline behaviors** — Add cross-cutting concerns like logging, validation, caching
- **High performance** — Compiled expression trees, no runtime reflection after first call
- **Full async/cancellation support**

## Installation

```bash
dotnet add package MiniatR
```

## Usage

### Define a Request and Handler

```csharp
public sealed record GetUser(Guid Id) : IRequest<User>;

public sealed class GetUserHandler : IRequestHandler<GetUser, User>
{
    public Task<User> Handle(GetUser request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new User(request.Id, "John Doe"));
    }
}
```

### Register Services

```csharp
services.AddMiniatR(cfg => cfg
    .RegisterServicesFromAssemblyContaining<GetUser>());
```

### Send Requests

```csharp
public class UserService(ISender sender)
{
    public Task<User> GetUser(Guid id, CancellationToken ct = default)
        => sender.Send(new GetUser(id), ct);
}
```

### Void Requests

```csharp
public sealed record DeleteUser(Guid Id) : IRequest;

public sealed class DeleteUserHandler : IRequestHandler<DeleteUser>
{
    public Task Handle(DeleteUser request, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
```

### Pipeline Behaviors

Behaviors wrap request handling for cross-cutting concerns:

```csharp
public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        PipelineDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"Handling {typeof(TRequest).Name}");
        var response = await next(cancellationToken);
        Console.WriteLine($"Handled {typeof(TRequest).Name}");
        return response;
    }
}
```

```csharp
services.AddMiniatR(cfg => cfg
    .RegisterServicesFromAssemblyContaining<GetUser>()
    .AddBehavior(typeof(LoggingBehavior<,>)));
```

### Configuration Options

```csharp
services.AddMiniatR(cfg => cfg
    .RegisterServicesFromAssemblyContaining<GetUser>()
    .RegisterServicesFromAssembly(typeof(OtherHandler).Assembly)
    .WithHandlerLifetime(ServiceLifetime.Transient)
    .WithBehaviorLifetime(ServiceLifetime.Scoped)
    .AddBehavior(typeof(LoggingBehavior<,>))
    .AddBehavior<GetUser, User, CachingBehavior>());
```

## License

MIT
