namespace MiniatR.Abstractions.Exceptions;

public sealed class HandlerNotFoundException(Type requestType, Type? responseType)
    : InvalidOperationException(CreateMessage(requestType, responseType))
{
    public Type RequestType { get; } = requestType;
    public Type? ResponseType { get; } = responseType;

    private static string CreateMessage(Type requestType, Type? responseType)
    {
        if (responseType == typeof(void) || responseType == null)
            return $"No handler registered for {requestType.Name}. Expected: IRequestHandler<{requestType.Name}>";

        return $"No handler registered for {requestType.Name}. Expected: IRequestHandler<{requestType.Name}, {responseType.Name}>";
    }
}
