using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace MiniatR.Extensions;

/// <summary>
/// Fluent configuration for MiniatR services.
/// </summary>
public sealed class MiniatRConfiguration
{
    private readonly IServiceCollection _services;
    private static readonly Type OpenBehaviorType = typeof(IPipelineBehavior<,>);

    internal HashSet<Assembly> Assemblies { get; } = [];
    internal ServiceLifetime HandlerLifetime { get; private set; } = ServiceLifetime.Scoped;
    internal ServiceLifetime BehaviorLifetime { get; private set; } = ServiceLifetime.Scoped;

    internal MiniatRConfiguration(IServiceCollection services)
    {
        _services = services;
    }

    /// <summary>
    /// Registers handlers from the assembly containing the specified type.
    /// </summary>
    public MiniatRConfiguration RegisterServicesFromAssemblyContaining<T>()
    {
        Assemblies.Add(typeof(T).Assembly);
        return this;
    }

    /// <summary>
    /// Registers handlers from the specified assembly.
    /// </summary>
    public MiniatRConfiguration RegisterServicesFromAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        Assemblies.Add(assembly);
        return this;
    }

    /// <summary>
    /// Sets the service lifetime for handlers. Default is Scoped.
    /// </summary>
    public MiniatRConfiguration WithHandlerLifetime(ServiceLifetime lifetime)
    {
        HandlerLifetime = lifetime;
        return this;
    }

    /// <summary>
    /// Sets the default service lifetime for behaviors. Default is Scoped.
    /// Individual behaviors can override this using the overloads that accept a lifetime parameter.
    /// </summary>
    public MiniatRConfiguration WithBehaviorLifetime(ServiceLifetime lifetime)
    {
        BehaviorLifetime = lifetime;
        return this;
    }

    /// <summary>
    /// Registers a pipeline behavior by type using the default behavior lifetime.
    /// </summary>
    public MiniatRConfiguration AddBehavior(Type behaviorType) => AddBehavior(behaviorType, BehaviorLifetime);

    /// <summary>
    /// Registers a pipeline behavior by type with a specific lifetime.
    /// </summary>
    /// <param name="behaviorType">The behavior type to register.</param>
    /// <param name="lifetime">The service lifetime for this behavior.</param>
    public MiniatRConfiguration AddBehavior(Type behaviorType, ServiceLifetime lifetime)
    {
        ValidateBehaviorType(behaviorType);

        if (behaviorType.IsGenericTypeDefinition)
        {
            _services.Add(new ServiceDescriptor(OpenBehaviorType, behaviorType, lifetime));
        }
        else
        {
            var behaviorInterface = behaviorType.GetInterfaces()
                .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == OpenBehaviorType);
            _services.Add(new ServiceDescriptor(behaviorInterface, behaviorType, lifetime));
        }

        return this;
    }

    /// <summary>
    /// Registers a pipeline behavior using the default behavior lifetime.
    /// </summary>
    public MiniatRConfiguration AddBehavior<TBehavior>() where TBehavior : class
        => AddBehavior<TBehavior>(BehaviorLifetime);

    /// <summary>
    /// Registers a pipeline behavior with a specific lifetime.
    /// </summary>
    /// <typeparam name="TBehavior">The behavior type to register.</typeparam>
    /// <param name="lifetime">The service lifetime for this behavior.</param>
    public MiniatRConfiguration AddBehavior<TBehavior>(ServiceLifetime lifetime) where TBehavior : class
    {
        var behaviorType = typeof(TBehavior);
        ValidateBehaviorType(behaviorType);

        var behaviorInterface = behaviorType.GetInterfaces()
            .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == OpenBehaviorType);
        _services.Add(new ServiceDescriptor(behaviorInterface, behaviorType, lifetime));

        return this;
    }

    /// <summary>
    /// Registers a strongly-typed pipeline behavior for a specific request/response pair using the default behavior lifetime.
    /// </summary>
    public MiniatRConfiguration AddBehavior<TRequest, TResponse, TBehavior>()
        where TRequest : IRequest<TResponse>
        where TBehavior : class, IPipelineBehavior<TRequest, TResponse>
        => AddBehavior<TRequest, TResponse, TBehavior>(BehaviorLifetime);

    /// <summary>
    /// Registers a strongly-typed pipeline behavior for a specific request/response pair with a specific lifetime.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <typeparam name="TBehavior">The behavior type to register.</typeparam>
    /// <param name="lifetime">The service lifetime for this behavior.</param>
    public MiniatRConfiguration AddBehavior<TRequest, TResponse, TBehavior>(ServiceLifetime lifetime)
        where TRequest : IRequest<TResponse>
        where TBehavior : class, IPipelineBehavior<TRequest, TResponse>
    {
        _services.Add(new ServiceDescriptor(typeof(IPipelineBehavior<TRequest, TResponse>), typeof(TBehavior), lifetime));
        return this;
    }

    private static void ValidateBehaviorType(Type behaviorType)
    {
        ArgumentNullException.ThrowIfNull(behaviorType);

        if (behaviorType.IsGenericTypeDefinition)
        {
            var implementsOpenBehavior = behaviorType.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == OpenBehaviorType);

            if (!implementsOpenBehavior)
            {
                throw new ArgumentException(
                    $"Type '{behaviorType.FullName}' does not implement IPipelineBehavior<TRequest, TResponse>. " +
                    "Open generic behaviors must implement the IPipelineBehavior<,> interface.",
                    nameof(behaviorType));
            }
        }
        else
        {
            var implementsClosedBehavior = behaviorType.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == OpenBehaviorType);

            if (!implementsClosedBehavior)
            {
                throw new ArgumentException(
                    $"Type '{behaviorType.FullName}' does not implement IPipelineBehavior<TRequest, TResponse>. " +
                    "Behaviors must implement the IPipelineBehavior<,> interface.",
                    nameof(behaviorType));
            }
        }
    }
}
