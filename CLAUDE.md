# MiniatR

Lightweight mediator library for .NET implementing the mediator/CQRS pattern.

## Project Structure

```
MiniatR/
├── MiniatR/                    # Main library
│   ├── IRequest.cs             # Request interfaces (IRequest<T>, IRequest)
│   ├── IRequestHandler.cs      # Handler interfaces
│   ├── ISender.cs              # Public interface for sending requests
│   ├── Sender.cs               # Internal ISender implementation
│   ├── InvokerCache.cs         # Compiled expression tree cache for handler/behavior invocation
│   ├── IPipelineBehavior.cs    # Pipeline behavior interface and delegate
│   ├── Nothing.cs              # Unit type for void requests
│   ├── Exceptions/
│   │   ├── HandlerNotFoundException.cs
│   │   └── DuplicateHandlerException.cs
│   └── Extensions/
│       ├── ServiceCollectionExtensions.cs  # DI registration (AddMiniatR)
│       └── MiniatRConfiguration.cs         # Fluent configuration
├── MiniatR.Tests/              # Test project
│   ├── Fixtures/               # Test helpers
│   │   ├── Requests.cs         # Test request records
│   │   ├── Handlers.cs         # Test handler implementations
│   │   ├── Behaviors.cs        # Test pipeline behaviors
│   │   └── TestCollections.cs  # xUnit test collections
│   ├── SenderTests.cs
│   ├── PipelineTests.cs
│   ├── RegistrationTests.cs
│   ├── CancellationTests.cs
│   ├── ConcurrencyTests.cs
│   └── NothingTests.cs
└── .github/workflows/ci.yml    # CI/CD pipeline
```

## Key Concepts

- **IRequest<TResponse>**: Request that returns a response
- **IRequest**: Void request (returns Nothing internally)
- **IRequestHandler<TRequest, TResponse>**: Handles requests with responses
- **IRequestHandler<TRequest>**: Handles void requests
- **ISender**: Sends requests through the pipeline
- **IPipelineBehavior<TRequest, TResponse>**: Middleware for cross-cutting concerns
- **PipelineDelegate<TResponse>**: Delegate to call next in pipeline (accepts CancellationToken)

## Commands

### Build
```bash
dotnet build
```

### Run Tests
```bash
dotnet test
```

### Run Tests with Code Coverage
```bash
# Generate coverage data
dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults

# Generate HTML report (requires reportgenerator tool)
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:"./TestResults/*/coverage.cobertura.xml" -targetdir:coverage-report -reporttypes:Html

# View summary
reportgenerator -reports:"./TestResults/*/coverage.cobertura.xml" -targetdir:coverage-report -reporttypes:TextSummary
cat coverage-report/Summary.txt
```

### Pack for NuGet
```bash
dotnet pack MiniatR/MiniatR.csproj --configuration Release --output ./nupkg
```

## Code Style

### Comments
- **NO code comments** (`//` style comments are forbidden in source code)
- **XML comments** (`///`) are allowed ONLY on public classes and their public members
- Internal/private code should be self-documenting through clear naming

### General
- Keep code minimal and focused
- Prefer expression-bodied members where appropriate
- Use primary constructors
- Use `ConfigureAwait(false)` on all async calls

## Test Guidelines

- Tests use xUnit v3 with `TestContext.Current.CancellationToken`
- Tests sharing static state use `[Collection("Sequential")]` to prevent parallelization issues
- Test fixtures are in `MiniatR.Tests/Fixtures/`
- Use `Theory` with `InlineData` for parameterized tests
- Reset static state at start of tests that depend on it

## Coverage Goals

- **Minimum 95% line coverage** (enforced in CI - build fails if below)
- Aim for 100% method coverage

## Architecture Notes

- All public types are in the root `MiniatR` namespace
- Extensions are in `MiniatR.Extensions` namespace
- Handlers are auto-discovered from registered assemblies
- Pipeline behaviors wrap handlers in registration order (first registered = outermost)
- Cancellation is checked at pipeline entry and before each behavior/handler execution
- Handler/behavior invocation uses compiled expression trees (cached per request type) for performance
- Behavior types are validated at registration time to ensure they implement `IPipelineBehavior<,>`
