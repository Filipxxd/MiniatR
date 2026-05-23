using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using MiniatR.Exceptions;

namespace MiniatR.Extensions;

/// <summary>
/// Extension methods for registering MiniatR services with dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers MiniatR services including the <see cref="ISender"/> and all handlers from configured assemblies.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configure">Action to configure MiniatR options including assembly registration and lifetimes.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no assemblies are registered.</exception>
    /// <exception cref="DuplicateHandlerException">Thrown when multiple handlers are found for the same request type.</exception>
    public static IServiceCollection AddMiniatR(this IServiceCollection services, Action<MiniatRConfiguration> configure)
    {
        var config = new MiniatRConfiguration(services);
        configure(config);

        services.AddScoped<ISender, Sender>();

        var allHandlers = new List<(Type Interface, Type Implementation)>();

        foreach (var assembly in config.Assemblies)
        {
            var handlers = FindHandlers(assembly);
            allHandlers.AddRange(handlers);
        }

        if (config.Assemblies.Count == 0)
            throw new InvalidOperationException("No assemblies registered. Call RegisterServicesFromAssemblyContaining<T>() or RegisterServicesFromAssembly().");

        ValidateNoDuplicates(allHandlers);

        foreach (var (iface, impl) in allHandlers)
            services.Add(new ServiceDescriptor(iface, impl, config.HandlerLifetime));

        return services;
    }

    private static IEnumerable<(Type Interface, Type Implementation)> FindHandlers(Assembly assembly)
    {
        var handlerWithResponse = typeof(IRequestHandler<,>);
        var handlerVoid = typeof(IRequestHandler<>);

        return assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false })
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType &&
                    (i.GetGenericTypeDefinition() == handlerWithResponse ||
                     i.GetGenericTypeDefinition() == handlerVoid))
                .Select(i => (Interface: i, Implementation: t)));
    }

    private static void ValidateNoDuplicates(List<(Type Interface, Type Implementation)> handlers)
    {
        var duplicates = handlers
            .GroupBy(h => h.Interface)
            .Where(g => g.Count() > 1)
            .Select(g => new DuplicateRegistration(
                g.Key.GetGenericArguments()[0],
                g.Select(h => h.Implementation).ToArray()))
            .ToList();

        if (duplicates.Count > 0)
            throw new DuplicateHandlerException(duplicates);
    }
}
