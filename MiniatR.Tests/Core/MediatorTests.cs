using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using MiniatR;
using MiniatR.Exceptions;
using MiniatR.Extensions;
using MiniatR.Tests.Fixtures;

namespace MiniatR.Tests.Core;

public sealed class MediatorTests
{
    [Fact]
    public async Task Send_WithValidHandler_ReturnsResponse()
    {
        var services = new ServiceCollection();
        services.AddMiniatR(cfg => cfg.RegisterServicesFromAssemblyContaining<GetUserQuery>());
        var provider = services.BuildServiceProvider();
        var sender = provider.GetRequiredService<ISender>();
        var userId = Guid.NewGuid();

        var response = await sender.Send(new GetUserQuery(userId));

        response.Should().NotBeNull();
        response.Id.Should().Be(userId);
        response.Name.Should().Be("Test User");
    }

    [Fact]
    public async Task Send_WithVoidHandler_Completes()
    {
        DeleteUserCommandHandler.Reset();
        var services = new ServiceCollection();
        services.AddMiniatR(cfg => cfg.RegisterServicesFromAssemblyContaining<DeleteUserCommand>());
        var provider = services.BuildServiceProvider();
        var sender = provider.GetRequiredService<ISender>();

        await sender.Send(new DeleteUserCommand(Guid.NewGuid()));

        DeleteUserCommandHandler.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task Send_WithNullRequest_ThrowsArgumentNull()
    {
        var services = new ServiceCollection();
        services.AddMiniatR(cfg => cfg.RegisterServicesFromAssemblyContaining<GetUserQuery>());
        var provider = services.BuildServiceProvider();
        var sender = provider.GetRequiredService<ISender>();

        await Assert.ThrowsAsync<ArgumentNullException>(() => sender.Send<UserResponse>(null!));
    }

    [Fact]
    public async Task Send_WithNoHandler_ThrowsHandlerNotFound()
    {
        var services = new ServiceCollection();
        services.AddMiniatR(cfg => cfg.RegisterServicesFromAssembly(typeof(MediatorTests).Assembly));
        var provider = services.BuildServiceProvider();
        var sender = provider.GetRequiredService<ISender>();

        var exception = await Assert.ThrowsAsync<HandlerNotFoundException>(() => sender.Send(new UnregisteredQuery()));

        exception.RequestType.Should().Be(typeof(UnregisteredQuery));
        exception.ResponseType.Should().Be(typeof(string));
    }

    [Fact]
    public async Task Send_WithCancellation_PropagatesToken()
    {
        var services = new ServiceCollection();
        services.AddMiniatR(cfg => cfg.RegisterServicesFromAssemblyContaining<SlowQuery>());
        var provider = services.BuildServiceProvider();
        var sender = provider.GetRequiredService<ISender>();
        using var cts = new CancellationTokenSource();

        var task = sender.Send(new SlowQuery(5000), cts.Token);
        await Task.Delay(50);
        cts.Cancel();

        await Assert.ThrowsAsync<TaskCanceledException>(() => task);
    }

    [Fact]
    public async Task Send_WithCancelledToken_ThrowsOperationCancelled()
    {
        var services = new ServiceCollection();
        services.AddMiniatR(cfg => cfg.RegisterServicesFromAssemblyContaining<SlowQuery>());
        var provider = services.BuildServiceProvider();
        var sender = provider.GetRequiredService<ISender>();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<TaskCanceledException>(() => sender.Send(new SlowQuery(100), cts.Token));
    }

    [Fact]
    public async Task Send_HandlerReturnsNull_ReturnsNull()
    {
        var services = new ServiceCollection();
        services.AddMiniatR(cfg => cfg.RegisterServicesFromAssemblyContaining<NullableQuery>());
        var provider = services.BuildServiceProvider();
        var sender = provider.GetRequiredService<ISender>();

        var response = await sender.Send(new NullableQuery());

        response.Should().BeNull();
    }

    [Fact]
    public async Task Send_HandlerThrows_PropagatesException()
    {
        var services = new ServiceCollection();
        services.AddMiniatR(cfg => cfg.RegisterServicesFromAssemblyContaining<ThrowingQuery>());
        var provider = services.BuildServiceProvider();
        var sender = provider.GetRequiredService<ISender>();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => sender.Send(new ThrowingQuery()));

        exception.Message.Should().Be("Handler threw an exception");
    }

    [Fact]
    public async Task Send_NestedSend_WorksCorrectly()
    {
        var services = new ServiceCollection();
        services.AddMiniatR(cfg => cfg.RegisterServicesFromAssemblyContaining<NestedQuery>());
        var provider = services.BuildServiceProvider();
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new NestedQuery(3));

        result.Should().Be(3);
    }

    [Fact]
    public async Task ISender_CanResolve_FromDI()
    {
        var services = new ServiceCollection();
        services.AddMiniatR(cfg => cfg.RegisterServicesFromAssemblyContaining<GetUserQuery>());
        var provider = services.BuildServiceProvider();

        var sender = provider.GetService<ISender>();

        sender.Should().NotBeNull();
    }

    [Fact]
    public async Task IMediator_CanResolve_FromDI()
    {
        var services = new ServiceCollection();
        services.AddMiniatR(cfg => cfg.RegisterServicesFromAssemblyContaining<GetUserQuery>());
        var provider = services.BuildServiceProvider();

        var mediator = provider.GetService<IMediator>();

        mediator.Should().NotBeNull();
    }

    [Fact]
    public async Task ISender_And_IMediator_AreSameInstance()
    {
        var services = new ServiceCollection();
        services.AddMiniatR(cfg => cfg.RegisterServicesFromAssemblyContaining<GetUserQuery>());
        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        sender.Should().BeSameAs(mediator);
    }
}

public sealed record UnregisteredQuery() : IRequest<string>;
