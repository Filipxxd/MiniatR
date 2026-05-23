using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using MiniatR;
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

    [Fact]
    public void ResponseHandler_RegistersCorrectly()
    {
        var services = new ServiceCollection();
        services.AddMiniatR(cfg => cfg.RegisterServicesFromAssemblyContaining<GetUserQuery>());
        var provider = services.BuildServiceProvider();

        var handler = provider.GetService<IRequestHandler<GetUserQuery, UserResponse>>();

        handler.Should().NotBeNull();
    }

    [Fact]
    public void VoidHandler_RegistersCorrectly()
    {
        var services = new ServiceCollection();
        services.AddMiniatR(cfg => cfg.RegisterServicesFromAssemblyContaining<DeleteUserCommand>());
        var provider = services.BuildServiceProvider();

        var handler = provider.GetService<IRequestHandler<DeleteUserCommand>>();

        handler.Should().NotBeNull();
    }

    [Theory]
    [InlineData(ServiceLifetime.Scoped)]
    [InlineData(ServiceLifetime.Transient)]
    [InlineData(ServiceLifetime.Singleton)]
    public void Lifetime_RegistersWithCorrectLifetime(ServiceLifetime lifetime)
    {
        var services = new ServiceCollection();
        services.AddMiniatR(cfg => cfg
            .RegisterServicesFromAssemblyContaining<GetUserQuery>()
            .WithHandlerLifetime(lifetime));

        var descriptor = services.First(s => s.ServiceType == typeof(IRequestHandler<GetUserQuery, UserResponse>));

        descriptor.Lifetime.Should().Be(lifetime);
    }

    [Fact]
    public void Transient_NewInstancePerResolve()
    {
        var services = new ServiceCollection();
        services.AddMiniatR(cfg => cfg
            .RegisterServicesFromAssemblyContaining<GetUserQuery>()
            .WithHandlerLifetime(ServiceLifetime.Transient));
        var provider = services.BuildServiceProvider();

        var handler1 = provider.GetRequiredService<IRequestHandler<GetUserQuery, UserResponse>>();
        var handler2 = provider.GetRequiredService<IRequestHandler<GetUserQuery, UserResponse>>();

        handler1.Should().NotBeSameAs(handler2);
    }

    [Fact]
    public void Singleton_SameInstanceAlways()
    {
        var services = new ServiceCollection();
        services.AddMiniatR(cfg => cfg
            .RegisterServicesFromAssemblyContaining<GetUserQuery>()
            .WithHandlerLifetime(ServiceLifetime.Singleton));
        var provider = services.BuildServiceProvider();

        var handler1 = provider.GetRequiredService<IRequestHandler<GetUserQuery, UserResponse>>();
        var handler2 = provider.GetRequiredService<IRequestHandler<GetUserQuery, UserResponse>>();

        handler1.Should().BeSameAs(handler2);
    }

    [Fact]
    public void Scoped_SameWithinScope_DifferentAcrossScopes()
    {
        var services = new ServiceCollection();
        services.AddMiniatR(cfg => cfg
            .RegisterServicesFromAssemblyContaining<GetUserQuery>()
            .WithHandlerLifetime(ServiceLifetime.Scoped));
        var provider = services.BuildServiceProvider();

        IRequestHandler<GetUserQuery, UserResponse> handler1, handler2, handler3;
        using (var scope1 = provider.CreateScope())
        {
            handler1 = scope1.ServiceProvider.GetRequiredService<IRequestHandler<GetUserQuery, UserResponse>>();
            handler2 = scope1.ServiceProvider.GetRequiredService<IRequestHandler<GetUserQuery, UserResponse>>();
        }
        using (var scope2 = provider.CreateScope())
        {
            handler3 = scope2.ServiceProvider.GetRequiredService<IRequestHandler<GetUserQuery, UserResponse>>();
        }

        handler1.Should().BeSameAs(handler2);
        handler1.Should().NotBeSameAs(handler3);
    }

    [Fact]
    public void NoAssemblies_Throws()
    {
        var services = new ServiceCollection();

        var ex = Assert.Throws<InvalidOperationException>(() => services.AddMiniatR(_ => { }));

        ex.Message.Should().Contain("No assemblies");
    }

    [Fact]
    public async Task HandlerWithDependencies_ResolvesCorrectly()
    {
        var services = new ServiceCollection();
        services.AddScoped<ITestDependency, TestDependency>();
        services.AddMiniatR(cfg => cfg.RegisterServicesFromAssemblyContaining<DependencyQuery>());
        var sender = services.BuildServiceProvider().GetRequiredService<ISender>();

        var result = await sender.Send(new DependencyQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.Value.Should().Be("From Dependency");
    }
}
