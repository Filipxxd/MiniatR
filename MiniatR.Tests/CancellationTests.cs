using Microsoft.Extensions.DependencyInjection;
using MiniatR.Extensions;
using MiniatR.Tests.Fixtures;

namespace MiniatR.Tests;

public sealed class CancellationTests
{
    [Fact]
    public async Task Send_CancelledToken_ThrowsOperationCancelledException()
    {
        var sender = CreateSender();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => sender.Send(new SlowQuery(100), cts.Token));
    }

    [Fact]
    public async Task Send_CancellationDuringExecution_ThrowsTaskCancelledException()
    {
        var sender = CreateSender();
        using var cts = new CancellationTokenSource();

        var task = sender.Send(new SlowQuery(5000), cts.Token);
        await Task.Delay(50, TestContext.Current.CancellationToken);
        cts.Cancel();

        await Assert.ThrowsAsync<TaskCanceledException>(() => task);
    }

    private static ISender CreateSender()
    {
        var services = new ServiceCollection();
        services.AddMiniatR(cfg => cfg.RegisterServicesFromAssemblyContaining<SlowQuery>());
        return services.BuildServiceProvider().GetRequiredService<ISender>();
    }
}
