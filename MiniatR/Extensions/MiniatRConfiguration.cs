using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using MiniatR.Abstractions;

namespace MiniatR.Extensions;

public sealed class MiniatRConfiguration
{
    private readonly IServiceCollection _services;
    internal List<Assembly> Assemblies { get; } = [];
    internal ServiceLifetime HandlerLifetime { get; private set; } = ServiceLifetime.Scoped;
    internal ServiceLifetime BehaviorLifetime { get; private set; } = ServiceLifetime.Scoped;

    internal MiniatRConfiguration(IServiceCollection services)
    {
        _services = services;
    }

    public MiniatRConfiguration RegisterServicesFromAssemblyContaining<T>()
    {
        Assemblies.Add(typeof(T).Assembly);
        return this;
    }

    public MiniatRConfiguration RegisterServicesFromAssembly(Assembly assembly)
    {
        Assemblies.Add(assembly);
        return this;
    }

    public MiniatRConfiguration WithHandlerLifetime(ServiceLifetime lifetime)
    {
        HandlerLifetime = lifetime;
        return this;
    }

    public MiniatRConfiguration WithBehaviorLifetime(ServiceLifetime lifetime)
    {
        BehaviorLifetime = lifetime;
        return this;
    }

    public MiniatRConfiguration AddBehavior(Type behaviorType)
    {
        _services.Add(new ServiceDescriptor(typeof(IPipelineBehavior<,>), behaviorType, BehaviorLifetime));
        return this;
    }

    public MiniatRConfiguration AddBehavior<TBehavior>() where TBehavior : class
    {
        _services.Add(new ServiceDescriptor(typeof(IPipelineBehavior<,>), typeof(TBehavior), BehaviorLifetime));
        return this;
    }

    public MiniatRConfiguration AddBehavior<TRequest, TResponse, TBehavior>()
        where TRequest : IRequest<TResponse>
        where TBehavior : class, IPipelineBehavior<TRequest, TResponse>
    {
        _services.Add(new ServiceDescriptor(typeof(IPipelineBehavior<TRequest, TResponse>), typeof(TBehavior), BehaviorLifetime));
        return this;
    }
}
