using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using MiniatR.Extensions;
using MiniatR.Tests.Fixtures;

namespace MiniatR.Tests;

[Collection("Sequential")]
public sealed class PipelineTests : IDisposable
{
    public PipelineTests()
    {
        LoggingBehavior<GetUserQuery, UserResponse>.Reset();
        LoggingBehavior<DeleteUserCommand, Nothing>.Reset();
        ShortCircuitBehavior<GetUserQuery, UserResponse>.Reset();
        ThrowingBehavior<GetUserQuery, UserResponse>.Reset();
        OrderTrackingBehavior<GetUserQuery, UserResponse>.Reset();
        DeleteUserCommandHandler.Reset();
    }

    public void Dispose() { }

    [Fact]
    public async Task NoBehaviors_CallsHandlerDirectly()
    {
        var sender = CreateSender();

        var response = await sender.Send(new GetUserQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

        response.Should().NotBeNull();
    }

    [Fact]
    public async Task SingleBehavior_WrapsHandler()
    {
        var sender = CreateSenderWithBehavior<LoggingBehavior<GetUserQuery, UserResponse>>();

        await sender.Send(new GetUserQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

        LoggingBehavior<GetUserQuery, UserResponse>.Log.Should().HaveCount(2);
        LoggingBehavior<GetUserQuery, UserResponse>.Log[0].Should().StartWith("Before:");
        LoggingBehavior<GetUserQuery, UserResponse>.Log[1].Should().StartWith("After:");
    }

    [Fact]
    public async Task MultipleBehaviors_ExecuteInRegistrationOrder()
    {
        var services = new ServiceCollection();
        services.AddMiniatR(cfg => cfg.RegisterServicesFromAssemblyContaining<GetUserQuery>());
        services.AddScoped<IPipelineBehavior<GetUserQuery, UserResponse>>(_ => new OrderTrackingBehavior<GetUserQuery, UserResponse>(1));
        services.AddScoped<IPipelineBehavior<GetUserQuery, UserResponse>>(_ => new OrderTrackingBehavior<GetUserQuery, UserResponse>(2));
        services.AddScoped<IPipelineBehavior<GetUserQuery, UserResponse>>(_ => new OrderTrackingBehavior<GetUserQuery, UserResponse>(3));
        var sender = services.BuildServiceProvider().GetRequiredService<ISender>();

        await sender.Send(new GetUserQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

        OrderTrackingBehavior<GetUserQuery, UserResponse>.ExecutionOrder.Should().Equal(
            "Behavior1-Before",
            "Behavior2-Before",
            "Behavior3-Before",
            "Behavior3-After",
            "Behavior2-After",
            "Behavior1-After");
    }

    [Fact]
    public async Task BehaviorThrows_PropagatesException()
    {
        ThrowingBehavior<GetUserQuery, UserResponse>.ShouldThrow = true;
        var sender = CreateSenderWithBehavior<ThrowingBehavior<GetUserQuery, UserResponse>>();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sender.Send(new GetUserQuery(Guid.NewGuid()), TestContext.Current.CancellationToken));

        ex.Message.Should().Be("Behavior threw an exception");
    }

    [Fact]
    public async Task BehaviorShortCircuits_HandlerNotCalled()
    {
        ShortCircuitBehavior<GetUserQuery, UserResponse>.ShouldShortCircuit = true;
        ShortCircuitBehavior<GetUserQuery, UserResponse>.ShortCircuitResponse = new UserResponse(Guid.Empty, "Cached");
        var sender = CreateSenderWithBehavior<ShortCircuitBehavior<GetUserQuery, UserResponse>>();

        var response = await sender.Send(new GetUserQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

        response.Name.Should().Be("Cached");
        ShortCircuitBehavior<GetUserQuery, UserResponse>.HandlerWasCalled.Should().BeFalse();
    }

    [Fact]
    public async Task VoidRequest_BehaviorsStillRun()
    {
        var services = new ServiceCollection();
        services.AddMiniatR(cfg => cfg.RegisterServicesFromAssemblyContaining<DeleteUserCommand>());
        services.AddScoped<IPipelineBehavior<DeleteUserCommand, Nothing>, LoggingBehavior<DeleteUserCommand, Nothing>>();
        var sender = services.BuildServiceProvider().GetRequiredService<ISender>();

        await sender.Send(new DeleteUserCommand(Guid.NewGuid()), TestContext.Current.CancellationToken);

        LoggingBehavior<DeleteUserCommand, Nothing>.Log.Should().HaveCount(2);
        DeleteUserCommandHandler.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task BehaviorWithDependencies_ResolvesCorrectly()
    {
        var services = new ServiceCollection();
        services.AddScoped<ITestDependency, TestDependency>();
        services.AddMiniatR(cfg => cfg.RegisterServicesFromAssemblyContaining<GetUserQuery>());
        services.AddScoped<IPipelineBehavior<GetUserQuery, UserResponse>, BehaviorWithDependencies<GetUserQuery, UserResponse>>();
        var sender = services.BuildServiceProvider().GetRequiredService<ISender>();

        var result = await sender.Send(new GetUserQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
    }

    private static ISender CreateSender()
    {
        var services = new ServiceCollection();
        services.AddMiniatR(cfg => cfg.RegisterServicesFromAssemblyContaining<GetUserQuery>());
        return services.BuildServiceProvider().GetRequiredService<ISender>();
    }

    private static ISender CreateSenderWithBehavior<TBehavior>() where TBehavior : class, IPipelineBehavior<GetUserQuery, UserResponse>
    {
        var services = new ServiceCollection();
        services.AddMiniatR(cfg => cfg.RegisterServicesFromAssemblyContaining<GetUserQuery>());
        services.AddScoped<IPipelineBehavior<GetUserQuery, UserResponse>, TBehavior>();
        return services.BuildServiceProvider().GetRequiredService<ISender>();
    }
}
