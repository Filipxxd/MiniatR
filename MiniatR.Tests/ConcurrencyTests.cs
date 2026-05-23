using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using MiniatR;
using MiniatR.Extensions;
using MiniatR.Tests.Fixtures;

namespace MiniatR.Tests;

public sealed class ConcurrencyTests
{
    [Theory]
    [InlineData(10)]
    [InlineData(50)]
    [InlineData(100)]
    public async Task Send_ConcurrentRequests_AllComplete(int count)
    {
        var sender = CreateSender();

        var tasks = Enumerable.Range(0, count)
            .Select(_ => sender.Send(new GetUserQuery(Guid.NewGuid()), TestContext.Current.CancellationToken));

        var results = await Task.WhenAll(tasks);

        results.Should().HaveCount(count);
        results.Should().AllSatisfy(r => r.Should().NotBeNull());
    }

    private static ISender CreateSender()
    {
        var services = new ServiceCollection();
        services.AddMiniatR(cfg => cfg.RegisterServicesFromAssemblyContaining<GetUserQuery>());
        return services.BuildServiceProvider().GetRequiredService<ISender>();
    }
}
