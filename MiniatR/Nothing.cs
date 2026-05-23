namespace MiniatR;

/// <summary>
/// Represents the absence of a value. Used as the response type for void requests.
/// </summary>
public readonly struct Nothing : IEquatable<Nothing>, IComparable<Nothing>
{
    /// <summary>
    /// The singleton value of <see cref="Nothing"/>.
    /// </summary>
    public static readonly Nothing Value = new();

    /// <summary>
    /// A completed task returning <see cref="Value"/>.
    /// </summary>
    public static readonly Task<Nothing> Task = System.Threading.Tasks.Task.FromResult(Value);

    public int CompareTo(Nothing other) => 0;
    public bool Equals(Nothing other) => true;
    public override bool Equals(object? obj) => obj is Nothing;
    public override int GetHashCode() => 0;
    public override string ToString() => string.Empty;
    public static bool operator ==(Nothing left, Nothing right) => true;
    public static bool operator !=(Nothing left, Nothing right) => false;
}
