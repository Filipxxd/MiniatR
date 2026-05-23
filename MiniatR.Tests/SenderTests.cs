using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using MiniatR;
using MiniatR.Exceptions;
using MiniatR.Extensions;
using MiniatR.Tests.Fixtures;

namespace MiniatR.Tests;

public sealed class SenderTests
{
    [Fact]
    public async Task Send_WithResponse_ReturnsExpectedResult()
    {
        var sender = CreateSender();
        var userId = Guid.NewGuid();

        var response = await sender.Send(new GetUserQuery(userId), TestContext.Current.CancellationToken);

        response.Should().NotBeNull();
        response.Id.Should().Be(userId);
    }

    [Fact]
    public async Task Send_WithoutResponse_CompletesSuccessfully()
    {
        DeleteUserCommandHandler.Reset();
        var sender = CreateSender();

        await sender.Send(new DeleteUserCommand(Guid.NewGuid()), TestContext.Current.CancellationToken);

        DeleteUserCommandHandler.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task Send_NullRequest_ThrowsArgumentNullException()
    {
        var sender = CreateSender();

        await Assert.ThrowsAsync<ArgumentNullException>(() => sender.Send<UserResponse>(null!, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Send_NoHandler_ThrowsHandlerNotFoundException()
    {
        var sender = CreateSender();

        var ex = await Assert.ThrowsAsync<HandlerNotFoundException>(() => sender.Send(new UnregisteredQuery(), TestContext.Current.CancellationToken));

        ex.RequestType.Should().Be(typeof(UnregisteredQuery));
    }

    [Fact]
    public async Task Send_HandlerReturnsNull_ReturnsNull()
    {
        var sender = CreateSender();

        var response = await sender.Send(new NullableQuery(), TestContext.Current.CancellationToken);

        response.Should().BeNull();
    }

    [Fact]
    public async Task Send_HandlerThrows_PropagatesException()
    {
        var sender = CreateSender();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sender.Send(new ThrowingQuery(), TestContext.Current.CancellationToken));

        ex.Message.Should().Be("Handler threw an exception");
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(3, 3)]
    [InlineData(5, 5)]
    public async Task Send_NestedCalls_ReturnsCorrectDepth(int depth, int expected)
    {
        var sender = CreateSender();

        var result = await sender.Send(new NestedQuery(depth), TestContext.Current.CancellationToken);

        result.Should().Be(expected);
    }

    private static ISender CreateSender()
    {
        var services = new ServiceCollection();
        services.AddMiniatR(cfg => cfg.RegisterServicesFromAssemblyContaining<GetUserQuery>());
        return services.BuildServiceProvider().GetRequiredService<ISender>();
    }
}
