namespace MiniatR.Exceptions;

public sealed class DuplicateHandlerException(Type requestType, Type[] handlerTypes)
    : InvalidOperationException($"Multiple handlers registered for {requestType.Name}: {string.Join(", ", handlerTypes.Select(t => t.Name))}")
{
    public Type RequestType { get; } = requestType;
    public Type[] HandlerTypes { get; } = handlerTypes;
}
