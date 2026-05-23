using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using MiniatR.Extensions;
using MiniatR.Tests.Fixtures;

namespace MiniatR.Tests;

public sealed class RegistrationTests
{
    [Fact]
    public void ISender_ResolvesFromDI()
    {
        var services = new ServiceCollection();
        services.AddMiniatR(cfg => cfg.RegisterServicesFromAssemblyContaining<GetUserQuery>());
        var provider = services.BuildServiceProvider();

        var sender = provider.GetService<ISender>();

        sender.Should().NotBeNull();
    }

    [Theory]
    [InlineData(ServiceLifetime.Scoped)]
    [InlineData(ServiceLifetime.Transient)]
    [InlineData(ServiceLifetime.Singleton)]
    public void Registration_WithLifetime_RegistersCorrectly(ServiceLifetime lifetime)
    {
        var services = new ServiceCollection();
        services.AddMiniatR(cfg => cfg
            .RegisterServicesFromAssemblyContaining<GetUserQuery>()
            .WithHandlerLifetime(lifetime));

        var descriptor = services.First(s => s.ServiceType == typeof(IRequestHandler<GetUserQuery, UserResponse>));

        descriptor.Lifetime.Should().Be(lifetime);
    }

    [Fact]
    public void Registration_NoAssemblies_Throws()
    {
        var services = new ServiceCollection();

        var ex = Assert.Throws<InvalidOperationException>(() => services.AddMiniatR(_ => { }));

        ex.Message.Should().Contain("No assemblies");
    }

    [Fact]
    public async Task Registration_HandlerWithDependencies_ResolvesCorrectly()
    {
        var services = new ServiceCollection();
        services.AddScoped<ITestDependency, TestDependency>();
        services.AddMiniatR(cfg => cfg.RegisterServicesFromAssemblyContaining<DependencyQuery>());
        var sender = services.BuildServiceProvider().GetRequiredService<ISender>();

        var result = await sender.Send(new DependencyQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.Value.Should().Be("From Dependency");
    }
}
