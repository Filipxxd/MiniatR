using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using MiniatR.Abstractions;
using MiniatR.Extensions;
using MiniatR.Tests.Fixtures;

namespace MiniatR.Tests.Pipeline;

[Collection("Sequential")]
public sealed class PipelineBehaviorTests
{
    [Fact]
    public async Task Pipeline_NoBehaviors_CallsHandlerDirectly()
    {
        var services = new ServiceCollection();
        services.AddMiniatR(cfg => cfg.RegisterServicesFromAssemblyContaining<GetUserQuery>());
        var provider = services.BuildServiceProvider();
        var sender = provider.GetRequiredService<ISender>();

        var response = await sender.Send(new GetUserQuery(Guid.NewGuid()));

        response.Should().NotBeNull();
    }

    [Fact]
    public async Task Pipeline_SingleBehavior_WrapsHandler()
    {
        LoggingBehavior<GetUserQuery, UserResponse>.Reset();
        var services = new ServiceCollection();
        services.AddMiniatR(cfg => cfg.RegisterServicesFromAssemblyContaining<GetUserQuery>());
        services.AddScoped<IPipelineBehavior<GetUserQuery, UserResponse>, LoggingBehavior<GetUserQuery, UserResponse>>();
        var provider = services.BuildServiceProvider();
        var sender = provider.GetRequiredService<ISender>();

        await sender.Send(new GetUserQuery(Guid.NewGuid()));

        LoggingBehavior<GetUserQuery, UserResponse>.Log.Should().HaveCount(2);
        LoggingBehavior<GetUserQuery, UserResponse>.Log[0].Should().StartWith("Before:");
        LoggingBehavior<GetUserQuery, UserResponse>.Log[1].Should().StartWith("After:");
    }

    [Fact]
    public async Task Pipeline_MultipleBehaviors_ExecuteInOrder()
    {
        OrderTrackingBehavior1<GetUserQuery, UserResponse>.Reset();
        var services = new ServiceCollection();
        services.AddMiniatR(cfg => cfg.RegisterServicesFromAssemblyContaining<GetUserQuery>());
        services.AddScoped<IPipelineBehavior<GetUserQuery, UserResponse>, OrderTrackingBehavior1<GetUserQuery, UserResponse>>();
        services.AddScoped<IPipelineBehavior<GetUserQuery, UserResponse>, OrderTrackingBehavior2<GetUserQuery, UserResponse>>();
        services.AddScoped<IPipelineBehavior<GetUserQuery, UserResponse>, OrderTrackingBehavior3<GetUserQuery, UserResponse>>();
        var provider = services.BuildServiceProvider();
        var sender = provider.GetRequiredService<ISender>();

        await sender.Send(new GetUserQuery(Guid.NewGuid()));

        var log = OrderTrackingBehavior1<GetUserQuery, UserResponse>.ExecutionOrder;
        log.Should().Equal(
            "Behavior1-Before",
            "Behavior2-Before",
            "Behavior3-Before",
            "Behavior3-After",
            "Behavior2-After",
            "Behavior1-After");
    }

    [Fact]
    public async Task Pipeline_BehaviorThrows_PropagatesException()
    {
        ThrowingBehavior<GetUserQuery, UserResponse>.Reset();
        ThrowingBehavior<GetUserQuery, UserResponse>.ShouldThrow = true;
        var services = new ServiceCollection();
        services.AddMiniatR(cfg => cfg.RegisterServicesFromAssemblyContaining<GetUserQuery>());
        services.AddScoped<IPipelineBehavior<GetUserQuery, UserResponse>, ThrowingBehavior<GetUserQuery, UserResponse>>();
        var provider = services.BuildServiceProvider();
        var sender = provider.GetRequiredService<ISender>();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => sender.Send(new GetUserQuery(Guid.NewGuid())));

        exception.Message.Should().Be("Behavior threw an exception");
    }

    [Fact]
    public async Task Pipeline_BehaviorSkipsNext_HandlerNotCalled()
    {
        ShortCircuitBehavior<GetUserQuery, UserResponse>.Reset();
        ShortCircuitBehavior<GetUserQuery, UserResponse>.ShouldShortCircuit = true;
        ShortCircuitBehavior<GetUserQuery, UserResponse>.ShortCircuitResponse = new UserResponse(Guid.Empty, "Short Circuit");

        var services = new ServiceCollection();
        services.AddMiniatR(cfg => cfg.RegisterServicesFromAssemblyContaining<GetUserQuery>());
        services.AddScoped<IPipelineBehavior<GetUserQuery, UserResponse>, ShortCircuitBehavior<GetUserQuery, UserResponse>>();
        var provider = services.BuildServiceProvider();
        var sender = provider.GetRequiredService<ISender>();

        var response = await sender.Send(new GetUserQuery(Guid.NewGuid()));

        response.Name.Should().Be("Short Circuit");
        ShortCircuitBehavior<GetUserQuery, UserResponse>.HandlerWasCalled.Should().BeFalse();
    }

    [Fact]
    public async Task Order_FirstRegistered_ExecutesFirst()
    {
        OrderTrackingBehavior1<GetUserQuery, UserResponse>.Reset();
        var services = new ServiceCollection();
        services.AddMiniatR(cfg => cfg.RegisterServicesFromAssemblyContaining<GetUserQuery>());
        services.AddScoped<IPipelineBehavior<GetUserQuery, UserResponse>, OrderTrackingBehavior1<GetUserQuery, UserResponse>>();
        services.AddScoped<IPipelineBehavior<GetUserQuery, UserResponse>, OrderTrackingBehavior2<GetUserQuery, UserResponse>>();
        var provider = services.BuildServiceProvider();
        var sender = provider.GetRequiredService<ISender>();

        await sender.Send(new GetUserQuery(Guid.NewGuid()));

        var log = OrderTrackingBehavior1<GetUserQuery, UserResponse>.ExecutionOrder;
        log[0].Should().Be("Behavior1-Before");
    }

    [Fact]
    public async Task Order_LastRegistered_ExecutesLast()
    {
        OrderTrackingBehavior1<GetUserQuery, UserResponse>.Reset();
        var services = new ServiceCollection();
        services.AddMiniatR(cfg => cfg.RegisterServicesFromAssemblyContaining<GetUserQuery>());
        services.AddScoped<IPipelineBehavior<GetUserQuery, UserResponse>, OrderTrackingBehavior1<GetUserQuery, UserResponse>>();
        services.AddScoped<IPipelineBehavior<GetUserQuery, UserResponse>, OrderTrackingBehavior2<GetUserQuery, UserResponse>>();
        var provider = services.BuildServiceProvider();
        var sender = provider.GetRequiredService<ISender>();

        await sender.Send(new GetUserQuery(Guid.NewGuid()));

        var log = OrderTrackingBehavior1<GetUserQuery, UserResponse>.ExecutionOrder;
        log[1].Should().Be("Behavior2-Before");
    }

    [Fact]
    public async Task Order_ReturnPath_ReversesOrder()
    {
        OrderTrackingBehavior1<GetUserQuery, UserResponse>.Reset();
        var services = new ServiceCollection();
        services.AddMiniatR(cfg => cfg.RegisterServicesFromAssemblyContaining<GetUserQuery>());
        services.AddScoped<IPipelineBehavior<GetUserQuery, UserResponse>, OrderTrackingBehavior1<GetUserQuery, UserResponse>>();
        services.AddScoped<IPipelineBehavior<GetUserQuery, UserResponse>, OrderTrackingBehavior2<GetUserQuery, UserResponse>>();
        var provider = services.BuildServiceProvider();
        var sender = provider.GetRequiredService<ISender>();

        await sender.Send(new GetUserQuery(Guid.NewGuid()));

        var log = OrderTrackingBehavior1<GetUserQuery, UserResponse>.ExecutionOrder;
        log[2].Should().Be("Behavior2-After");
        log[3].Should().Be("Behavior1-After");
    }

    [Fact]
    public async Task ClosedBehavior_OnlyRunsForSpecificRequest()
    {
        GetUserQueryLoggingBehavior.Reset();
        var services = new ServiceCollection();
        services.AddMiniatR(cfg => cfg
            .RegisterServicesFromAssemblyContaining<GetUserQuery>()
            .AddBehavior<GetUserQuery, UserResponse, GetUserQueryLoggingBehavior>());
        var provider = services.BuildServiceProvider();
        var sender = provider.GetRequiredService<ISender>();

        await sender.Send(new GetUserQuery(Guid.NewGuid()));

        GetUserQueryLoggingBehavior.Log.Should().HaveCount(2);
    }

    [Fact]
    public async Task NothingHandler_BehaviorsStillRun()
    {
        DeleteUserCommandHandler.Reset();
        LoggingBehavior<DeleteUserCommand, Nothing>.Reset();
        var services = new ServiceCollection();
        services.AddMiniatR(cfg => cfg.RegisterServicesFromAssemblyContaining<DeleteUserCommand>());
        services.AddScoped<IPipelineBehavior<DeleteUserCommand, Nothing>, LoggingBehavior<DeleteUserCommand, Nothing>>();
        var provider = services.BuildServiceProvider();
        var sender = provider.GetRequiredService<ISender>();

        await sender.Send(new DeleteUserCommand(Guid.NewGuid()));

        LoggingBehavior<DeleteUserCommand, Nothing>.Log.Should().HaveCount(2);
        DeleteUserCommandHandler.CallCount.Should().Be(1);
    }
}

[CollectionDefinition("Sequential", DisableParallelization = true)]
public class SequentialCollection { }
