# MiniatR

Minimal mediator implementation for .NET.

## Installation

```bash
dotnet add package MiniatR
```

## Quick Start

```csharp
using MiniatR.Abstractions;
using MiniatR.Extensions;

// Define request and handler
public sealed record GetUserQuery(Guid Id) : IRequest<UserResponse>;
public sealed record UserResponse(Guid Id, string Name);

public sealed class GetUserQueryHandler : IRequestHandler<GetUserQuery, UserResponse>
{
    public async Task<UserResponse> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        return new UserResponse(request.Id, "John Doe");
    }
}

// Register
services.AddMiniatR(cfg => cfg.RegisterServicesFromAssemblyContaining<GetUserQuery>());

// Use
public class MyService
{
    private readonly ISender _sender;
    public MyService(ISender sender) => _sender = sender;

    public async Task<UserResponse> GetUser(Guid id)
        => await _sender.Send(new GetUserQuery(id));
}
```

## Void Requests

```csharp
public sealed record DeleteUserCommand(Guid Id) : IRequest;

public sealed class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand>
{
    public async Task Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        // No return needed
    }
}

await sender.Send(new DeleteUserCommand(userId));
```

## Pipeline Behaviors

```csharp
public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, PipelineDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Handling {typeof(TRequest).Name}");
        var response = await next();
        Console.WriteLine($"Handled {typeof(TRequest).Name}");
        return response;
    }
}

// Register directly with DI
services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

// Or via config for closed generics
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

## Project Structure

```
MiniatR/
├── Abstractions/    # IRequest, IRequestHandler, ISender, IMediator, IPipelineBehavior, Nothing, Exceptions
├── Core/            # Mediator implementation
└── Extensions/      # ServiceCollectionExtensions, MiniatRConfiguration
```

## License

MIT
