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

    internal List<Assembly> Assemblies { get; } = [];
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
    /// Sets the service lifetime for behaviors. Default is Scoped.
    /// </summary>
    public MiniatRConfiguration WithBehaviorLifetime(ServiceLifetime lifetime)
    {
        BehaviorLifetime = lifetime;
        return this;
    }

    /// <summary>
    /// Registers a pipeline behavior by type. Validates that the type implements IPipelineBehavior.
    /// </summary>
    public MiniatRConfiguration AddBehavior(Type behaviorType)
    {
        ValidateBehaviorType(behaviorType);
        _services.Add(new ServiceDescriptor(OpenBehaviorType, behaviorType, BehaviorLifetime));
        return this;
    }

    /// <summary>
    /// Registers a pipeline behavior.
    /// </summary>
    public MiniatRConfiguration AddBehavior<TBehavior>() where TBehavior : class
    {
        return AddBehavior(typeof(TBehavior));
    }

    /// <summary>
    /// Registers a strongly-typed pipeline behavior for a specific request/response pair.
    /// </summary>
    public MiniatRConfiguration AddBehavior<TRequest, TResponse, TBehavior>()
        where TRequest : IRequest<TResponse>
        where TBehavior : class, IPipelineBehavior<TRequest, TResponse>
    {
        _services.Add(new ServiceDescriptor(typeof(IPipelineBehavior<TRequest, TResponse>), typeof(TBehavior), BehaviorLifetime));
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
