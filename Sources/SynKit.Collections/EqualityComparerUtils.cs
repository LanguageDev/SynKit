using System.Diagnostics.CodeAnalysis;

namespace SynKit.Collections;

/// <summary>
/// Utility functions for constructing <see cref="IEqualityComparer{T}"/>s.
/// </summary>
public static class EqualityComparerUtils
{
    private sealed class LambdaEqualityComparer<T> : IEqualityComparer<T>
    {
        public Func<T?, T?, bool> Equality { get; }

        public Func<T, int> Hash { get; }

        public LambdaEqualityComparer(Func<T?, T?, bool> equality, Func<T, int> hash)
        {
            this.Equality = equality;
            this.Hash = hash;
        }

        public bool Equals(T? x, T? y) => this.Equality(x, y);

        public int GetHashCode([DisallowNull] T obj) => this.Hash(obj);
    }

    /// <summary>
    /// Constructs a <see cref="KeyValuePair{TKey, TValue}"/> equality comparer.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <param name="keyComparer">The first element comparer.</param>
    /// <param name="valueComparer">The second element comparer.</param>
    /// <returns>An equality comparer that compares the <see cref="KeyValuePair{TKey, TValue}"/>
    /// elements with the given comparers.</returns>
    public static IEqualityComparer<KeyValuePair<TKey, TValue>> KeyValuePairEqualityComparer<TKey, TValue>(
        IEqualityComparer<TKey>? keyComparer = null,
        IEqualityComparer<TValue>? valueComparer = null)
    {
        var cmpKey = keyComparer ?? EqualityComparer<TKey>.Default;
        var cmpValue = valueComparer ?? EqualityComparer<TValue>.Default;
        return new LambdaEqualityComparer<KeyValuePair<TKey, TValue>>(
            (x, y) => cmpKey.Equals(x.Key, y.Key) && cmpValue.Equals(x.Value, y.Value),
            x =>
            {
                var h = default(HashCode);
                h.Add(x.Key, keyComparer);
                h.Add(x.Value, valueComparer);
                return h.ToHashCode();
            });
    }

    /// <summary>
    /// Constructs a tuple equality comparer.
    /// </summary>
    /// <typeparam name="T1">The first element type.</typeparam>
    /// <typeparam name="T2">The second element type.</typeparam>
    /// <param name="first">The first element comparer.</param>
    /// <param name="second">The second element comparer.</param>
    /// <returns>An equality comparer that compares the tuple elements with the given comparers.</returns>
    public static IEqualityComparer<(T1, T2)> TupleEqualityComparer<T1, T2>(
        IEqualityComparer<T1>? first = null,
        IEqualityComparer<T2>? second = null)
    {
        var cmp1 = first ?? EqualityComparer<T1>.Default;
        var cmp2 = second ?? EqualityComparer<T2>.Default;
        return new LambdaEqualityComparer<(T1, T2)>(
            (x, y) => cmp1.Equals(x.Item1, y.Item1) && cmp2.Equals(x.Item2, y.Item2),
            x =>
            {
                var h = default(HashCode);
                h.Add(x.Item1, cmp1);
                h.Add(x.Item2, cmp2);
                return h.ToHashCode();
            });
    }

    /// <summary>
    /// Constructs a set equality comparer that compares two sequences order-independently.
    /// </summary>
    /// <typeparam name="T">The element type of the sequence.</typeparam>
    /// <param name="elementComparer">The comparer to compare elements with.</param>
    /// <returns>An <see cref="IEqualityComparer{T}"/> that compares elements order independently.</returns>
    public static IEqualityComparer<IEnumerable<T>> SetEqualityComparer<T>(IEqualityComparer<T>? elementComparer = null)
    {
        var cmp = elementComparer ?? EqualityComparer<T>.Default;
        return new LambdaEqualityComparer<IEnumerable<T>>(
            (x, y) =>
            {
                if (ReferenceEquals(x, y)) return true;
                if (x is null || y is null) return false;
                var hasCount1 = x.TryGetNonEnumeratedCount(out var count1);
                var hasCount2 = y.TryGetNonEnumeratedCount(out var count2);
                if (hasCount1 && hasCount2 && count1 != count2) return false;
                return x.ToHashSet(cmp).SetEquals(y);
            },
            x =>
            {
                // NOTE: Order-independent hash
                var hashCode = 0;
                foreach (var item in x)
                {
                    if (item is not null) hashCode ^= cmp.GetHashCode(item);
                }
                return hashCode;
            });
    }

    /// <summary>
    /// Constructs a sequence equality comparer that compares two sequences order-dependently.
    /// </summary>
    /// <typeparam name="T">The element type of the sequence.</typeparam>
    /// <param name="elementComparer">The comparer to compare elements with.</param>
    /// <returns>An <see cref="IEqualityComparer{T}"/> that compares elements order dependently.</returns>
    public static IEqualityComparer<IEnumerable<T>> SequenceEqualityComparer<T>(IEqualityComparer<T>? elementComparer = null)
    {
        var cmp = elementComparer ?? EqualityComparer<T>.Default;
        return new LambdaEqualityComparer<IEnumerable<T>>(
            (x, y) =>
            {
                if (ReferenceEquals(x, y)) return true;
                if (x is null || y is null) return false;
                var hasCount1 = x.TryGetNonEnumeratedCount(out var count1);
                var hasCount2 = y.TryGetNonEnumeratedCount(out var count2);
                if (hasCount1 && hasCount2 && count1 != count2) return false;
                return x.SequenceEqual(y, cmp);
            },
            x =>
            {
                var h = default(HashCode);
                foreach (var item in x) h.Add(item, cmp);
                return h.ToHashCode();
            });
    }
}
