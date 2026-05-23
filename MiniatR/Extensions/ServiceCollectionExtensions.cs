using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using MiniatR.Exceptions;

namespace MiniatR.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMiniatR(this IServiceCollection services, Action<MiniatRConfiguration> configure)
    {
        var config = new MiniatRConfiguration(services);
        configure(config);

        services.AddScoped<Mediator>();
        services.AddScoped<ISender>(sp => sp.GetRequiredService<Mediator>());
        services.AddScoped<IMediator>(sp => sp.GetRequiredService<Mediator>());

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
            .ToList();

        if (duplicates.Count > 0)
        {
            var first = duplicates[0];
            var requestType = first.Key.GetGenericArguments()[0];
            var handlerTypes = first.Select(h => h.Implementation).ToArray();
            throw new DuplicateHandlerException(requestType, handlerTypes);
        }
    }
}
