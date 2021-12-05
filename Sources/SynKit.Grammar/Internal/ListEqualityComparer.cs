using System.Diagnostics.CodeAnalysis;

namespace SynKit.Grammar.Internal;

internal sealed class ListEqualityComparer<T> : IEqualityComparer<IReadOnlyList<T>>
{
    public static ListEqualityComparer<T> Default { get; } = new(EqualityComparer<T>.Default);

    private readonly IEqualityComparer<T> elementComparer;

    public ListEqualityComparer(IEqualityComparer<T> elementComparer)
    {
        this.elementComparer = elementComparer;
    }

    public bool Equals(IReadOnlyList<T>? x, IReadOnlyList<T>? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;
        if (x.Count != y.Count) return false;
        return x.SequenceEqual(y, this.elementComparer);
    }

    public int GetHashCode([DisallowNull] IReadOnlyList<T> obj)
    {
        var h = default(HashCode);
        foreach (var item in obj) h.Add(item, this.elementComparer);
        return h.ToHashCode();
    }
}
