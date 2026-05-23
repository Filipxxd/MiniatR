namespace MiniatR.Exceptions;

public sealed class HandlerNotFoundException(Type requestType, Type? responseType)
    : InvalidOperationException(CreateMessage(requestType, responseType))
{
    public Type RequestType { get; } = requestType;
    public Type? ResponseType { get; } = responseType;

    private static string CreateMessage(Type requestType, Type? responseType)
    {
        var requestTypeName = FormatTypeName(requestType);

        if (responseType == typeof(void) || responseType == null)
        {
            return $"No handler registered for request '{requestTypeName}'. " +
                   $"Expected handler implementing: IRequestHandler<{requestType.Name}>. " +
                   "Ensure the handler is in an assembly registered via RegisterServicesFromAssembly() or RegisterServicesFromAssemblyContaining<T>().";
        }

        var responseTypeName = FormatTypeName(responseType);
        return $"No handler registered for request '{requestTypeName}' with response '{responseTypeName}'. " +
               $"Expected handler implementing: IRequestHandler<{requestType.Name}, {responseType.Name}>. " +
               "Ensure the handler is in an assembly registered via RegisterServicesFromAssembly() or RegisterServicesFromAssemblyContaining<T>().";
    }

    private static string FormatTypeName(Type type)
    {
        if (!type.IsGenericType)
            return type.FullName ?? type.Name;

        var genericTypeName = type.GetGenericTypeDefinition().FullName ?? type.GetGenericTypeDefinition().Name;
        var backtickIndex = genericTypeName.IndexOf('`');
        if (backtickIndex > 0)
            genericTypeName = genericTypeName[..backtickIndex];

        var genericArgs = string.Join(", ", type.GetGenericArguments().Select(FormatTypeName));
        return $"{genericTypeName}<{genericArgs}>";
    }
}
