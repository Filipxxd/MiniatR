namespace MiniatR;

public readonly struct Nothing : IEquatable<Nothing>, IComparable<Nothing>
{
    public static readonly Nothing Value = new();
    public static readonly Task<Nothing> Task = System.Threading.Tasks.Task.FromResult(Value);

    public int CompareTo(Nothing other) => 0;
    public bool Equals(Nothing other) => true;
    public override bool Equals(object? obj) => obj is Nothing;
    public override int GetHashCode() => 0;
    public override string ToString() => "()";
    public static bool operator ==(Nothing left, Nothing right) => true;
    public static bool operator !=(Nothing left, Nothing right) => false;
}
