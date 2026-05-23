using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using MiniatR;
using MiniatR.Extensions;
using MiniatR.Tests.Fixtures;

namespace MiniatR.Tests.Registration;

public sealed class RegistrationTests
{
    [Fact]
    public void RegisterHandlers_ScansAssembly_RegistersAllHandlers()
    {
        var services = new ServiceCollection();

        services.AddMiniatR(cfg => cfg.RegisterServicesFromAssemblyContaining<GetUserQuery>());

        var provider = services.BuildServiceProvider();
        var handler = provider.GetService<IRequestHandler<GetUserQuery, UserResponse>>();
        handler.Should().NotBeNull();
    }

    [Fact]
    public void RegisterHandlers_VoidHandler_Registered()
    {
        var services = new ServiceCollection();

        services.AddMiniatR(cfg => cfg.RegisterServicesFromAssemblyContaining<DeleteUserCommand>());

        var provider = services.BuildServiceProvider();
        var handler = provider.GetService<IRequestHandler<DeleteUserCommand>>();
        handler.Should().NotBeNull();
    }

    [Fact]
    public void RegisterHandlers_NoAssemblies_Throws()
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(() => services.AddMiniatR(cfg => { }));

        exception.Message.Should().Contain("No assemblies registered");
    }

    [Fact]
    public void RegisterHandlers_DuplicateHandler_Throws()
    {
        var services = new ServiceCollection();
        services.AddMiniatR(cfg => cfg.RegisterServicesFromAssemblyContaining<GetUserQuery>());

        var provider = services.BuildServiceProvider();
        var handler = provider.GetService<IRequestHandler<GetUserQuery, UserResponse>>();
        handler.Should().NotBeNull();
    }

    [Fact]
    public void RegisterHandlers_ScopedLifetime_Default()
    {
        var services = new ServiceCollection();

        services.AddMiniatR(cfg => cfg.RegisterServicesFromAssemblyContaining<GetUserQuery>());

        var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IRequestHandler<GetUserQuery, UserResponse>));
        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    [Fact]
    public void RegisterHandlers_TransientLifetime_NewPerResolve()
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
    public void RegisterHandlers_SingletonLifetime_SameInstance()
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
    public void RegisterHandlers_ScopedLifetime_NewPerScope()
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
    public async Task RegisterHandlers_HandlerWithDependencies_Resolved()
    {
        var services = new ServiceCollection();
        services.AddScoped<ITestDependency, TestDependency>();
        services.AddMiniatR(cfg => cfg.RegisterServicesFromAssemblyContaining<DependencyQuery>());
        var provider = services.BuildServiceProvider();
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new DependencyQuery(Guid.NewGuid()));

        result.Should().NotBeNull();
        result.Value.Should().Be("From Dependency");
    }

    [Fact]
    public async Task RegisterHandlers_BehaviorWithDependencies_Resolved()
    {
        var services = new ServiceCollection();
        services.AddScoped<ITestDependency, TestDependency>();
        services.AddMiniatR(cfg => cfg.RegisterServicesFromAssemblyContaining<GetUserQuery>());
        services.AddScoped<IPipelineBehavior<GetUserQuery, UserResponse>, BehaviorWithDependencies<GetUserQuery, UserResponse>>();
        var provider = services.BuildServiceProvider();
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new GetUserQuery(Guid.NewGuid()));

        result.Should().NotBeNull();
    }

    [Fact]
    public void ISender_InjectIntoHandler_Works()
    {
        var services = new ServiceCollection();
        services.AddMiniatR(cfg => cfg.RegisterServicesFromAssemblyContaining<NestedQuery>());
        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IRequestHandler<NestedQuery, int>>();

        handler.Should().BeOfType<NestedQueryHandler>();
    }
}
