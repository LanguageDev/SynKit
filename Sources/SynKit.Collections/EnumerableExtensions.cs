using System.Collections;

namespace SynKit.Collections;

/// <summary>
/// Extensions for <see cref="IEnumerable{T}"/>s.
/// </summary>
public static class EnumerableExtensions
{
    private sealed class SelectedCollection<T, U> : IReadOnlyCollection<U>
    {
        public int Count => this.collection.Count;

        private readonly IReadOnlyCollection<T> collection;
        private readonly Func<T, U> selector;

        public SelectedCollection(IReadOnlyCollection<T> collection, Func<T, U> selector)
        {
            this.collection = collection;
            this.selector = selector;
        }

        public IEnumerator<U> GetEnumerator() => this.collection.Select(this.selector).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }

    /// <summary>
    /// Equivalent to <see cref="Enumerable.Select{TSource, TResult}(IEnumerable{TSource}, Func{TSource, TResult})"/>,
    /// on an <see cref="IReadOnlyCollection{T}"/>, but keeps <see cref="IReadOnlyCollection{T}.Count"/> around.
    /// This is possible, because selecting does not drop or insert any elements, the element count is invariant.
    /// </summary>
    /// <typeparam name="T">The source collection element type.</typeparam>
    /// <typeparam name="U">The selected element type.</typeparam>
    /// <param name="collection">The collection to transform.</param>
    /// <param name="selector">The sepector function to transform elements with.</param>
    /// <returns>A collection, where each element of <paramref name="collection"/> is transformed using
    /// <paramref name="selector"/>.</returns>
    public static IReadOnlyCollection<U> SelectCollection<T, U>(
        this IReadOnlyCollection<T> collection,
        Func<T, U> selector) => new SelectedCollection<T, U>(collection, selector);

    /// <summary>
    /// Run-length encodes the given sequence of elements.
    /// </summary>
    /// <typeparam name="T">The element type of the sequence.</typeparam>
    /// <param name="enumerable">The sequence to encode.</param>
    /// <returns>A sequence of pairs of element and repetition count that represents <paramref name="enumerable"/>
    /// in RLE.</returns>
    public static IEnumerable<(T Element, int Count)> RunLengthEncode<T>(this IEnumerable<T> enumerable) =>
        RunLengthEncode(enumerable, EqualityComparer<T>.Default);

    /// <summary>
    /// Run-length encodes the given sequence of elements.
    /// </summary>
    /// <typeparam name="T">The element type of the sequence.</typeparam>
    /// <param name="enumerable">The sequence to encode.</param>
    /// <param name="comparer">The comparer to use when comparing elements for equality.</param>
    /// <returns>A sequence of pairs of element and repetition count that represents <paramref name="enumerable"/>
    /// in RLE.</returns>
    public static IEnumerable<(T Element, int Count)> RunLengthEncode<T>(
        this IEnumerable<T> enumerable,
        IEqualityComparer<T> comparer)
    {
        var enumerator = enumerable.GetEnumerator();
        if (!enumerator.MoveNext()) yield break;

        var prev = enumerator.Current;
        var count = 1;
        while (enumerator.MoveNext())
        {
            var current = enumerator.Current;
            if (comparer.Equals(prev, current))
            {
                ++count;
            }
            else
            {
                yield return (prev, count);
                count = 1;
                prev = current;
            }
        }

        yield return (prev, count);
    }
}
