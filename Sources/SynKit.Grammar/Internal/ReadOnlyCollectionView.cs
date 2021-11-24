using System.Collections;

namespace SynKit.Grammar.Internal;

internal record struct ReadOnlyCollectionView<TItem, TView>(
    IReadOnlyCollection<TItem> Items,
    Func<TItem, TView> View) : IReadOnlyCollection<TView>
{
    public int Count => this.Items.Count;

    public IEnumerator<TView> GetEnumerator() => this.Items.Select(this.View).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}
