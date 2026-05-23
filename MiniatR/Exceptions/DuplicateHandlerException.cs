namespace MiniatR.Exceptions;

/// <summary>
/// Exception thrown when multiple handlers are registered for one or more request types.
/// </summary>
public sealed class DuplicateHandlerException : InvalidOperationException
{
    /// <summary>
    /// All duplicate handler registrations found.
    /// </summary>
    public IReadOnlyList<DuplicateRegistration> Duplicates { get; }

    internal DuplicateHandlerException(IReadOnlyList<DuplicateRegistration> duplicates)
        : base(CreateMessage(duplicates))
    {
        Duplicates = duplicates;
    }

    private static string CreateMessage(IReadOnlyList<DuplicateRegistration> duplicates)
    {
        var lines = duplicates.Select(d =>
            $"  - {d.RequestType.Name}: [{string.Join(", ", d.HandlerTypes.Select(t => t.Name))}]");

        return $"Multiple handlers registered for {duplicates.Count} request type(s):\n{string.Join("\n", lines)}";
    }
}

/// <summary>
/// Represents a duplicate handler registration for a specific request type.
/// </summary>
/// <param name="RequestType">The request type that has multiple handlers.</param>
/// <param name="HandlerTypes">The handler types registered for this request.</param>
public sealed record DuplicateRegistration(Type RequestType, IReadOnlyList<Type> HandlerTypes);
