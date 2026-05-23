using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using MiniatR.Abstractions;
using MiniatR.Extensions;
using MiniatR.Tests.Fixtures;

namespace MiniatR.Tests.Core;

public sealed class ConcurrencyTests
{
    [Fact]
    public async Task Concurrent_MultipleSends_AllComplete()
    {
        var services = new ServiceCollection();
        services.AddMiniatR(cfg => cfg.RegisterServicesFromAssemblyContaining<GetUserQuery>());
        var provider = services.BuildServiceProvider();
        var sender = provider.GetRequiredService<ISender>();

        var tasks = Enumerable.Range(0, 100)
            .Select(_ => sender.Send(new GetUserQuery(Guid.NewGuid())))
            .ToList();

        var results = await Task.WhenAll(tasks);

        results.Should().HaveCount(100);
        results.Should().AllSatisfy(r => r.Should().NotBeNull());
    }

    [Fact]
    public async Task Concurrent_SameRequest_IndependentExecution()
    {
        var services = new ServiceCollection();
        services.AddMiniatR(cfg => cfg.RegisterServicesFromAssemblyContaining<GetUserQuery>());
        var provider = services.BuildServiceProvider();

        var userId = Guid.NewGuid();
        var request = new GetUserQuery(userId);

        var tasks = Enumerable.Range(0, 50)
            .Select(_ =>
            {
                using var scope = provider.CreateScope();
                var sender = scope.ServiceProvider.GetRequiredService<ISender>();
                return sender.Send(request);
            })
            .ToList();

        var results = await Task.WhenAll(tasks);

        results.Should().AllSatisfy(r => r.Id.Should().Be(userId));
    }

    [Fact]
    public async Task Concurrent_DifferentRequests_NoInterference()
    {
        var services = new ServiceCollection();
        services.AddMiniatR(cfg => cfg.RegisterServicesFromAssemblyContaining<GetUserQuery>());
        var provider = services.BuildServiceProvider();
        var sender = provider.GetRequiredService<ISender>();

        var userQueries = Enumerable.Range(0, 25)
            .Select(_ => sender.Send(new GetUserQuery(Guid.NewGuid())));

        var cacheableQueries = Enumerable.Range(0, 25)
            .Select(i => sender.Send(new CacheableQuery($"key-{i}")));

        var nonCacheableQueries = Enumerable.Range(0, 25)
            .Select(i => sender.Send(new NonCacheableQuery($"key-{i}")));

        var allTasks = userQueries
            .Cast<Task>()
            .Concat(cacheableQueries)
            .Concat(nonCacheableQueries)
            .ToList();

        await Task.WhenAll(allTasks);

        allTasks.Should().AllSatisfy(t => t.IsCompletedSuccessfully.Should().BeTrue());
    }

    [Fact]
    public async Task Concurrent_WithBehaviors_ThreadSafe()
    {
        var services = new ServiceCollection();
        services.AddMiniatR(cfg => cfg.RegisterServicesFromAssemblyContaining<GetUserQuery>());
        services.AddScoped<IPipelineBehavior<GetUserQuery, UserResponse>, LoggingBehavior<GetUserQuery, UserResponse>>();
        var provider = services.BuildServiceProvider();

        var tasks = Enumerable.Range(0, 100)
            .Select(_ =>
            {
                using var scope = provider.CreateScope();
                var sender = scope.ServiceProvider.GetRequiredService<ISender>();
                return sender.Send(new GetUserQuery(Guid.NewGuid()));
            })
            .ToList();

        var results = await Task.WhenAll(tasks);

        results.Should().HaveCount(100);
    }
}
