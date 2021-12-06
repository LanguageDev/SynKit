using System.Collections;

namespace SynKit.Grammar.Internal;

internal static class EnumerableExtensions
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

    public static IReadOnlyCollection<U> SelectCollection<T, U>(
        this IReadOnlyCollection<T> collection,
        Func<T, U> selector) => new SelectedCollection<T, U>(collection, selector);
}
