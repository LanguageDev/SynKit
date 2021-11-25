using SynKit.Grammar.Lr.Items;
using System.Collections;

namespace SynKit.Grammar.Lr.Internal;

internal sealed class LrStateAllocator<TItem> : IReadOnlyCollection<LrStateItemSet<TItem>>
    where TItem : class, ILrItem
{
    public int Count => this.States.Count;

    public IReadOnlyCollection<LrState> States => this.stateToItemSet.Keys;

    public IReadOnlyCollection<LrItemSet<TItem>> ItemSets => this.itemSetToState.Keys;

    public LrState this[LrItemSet<TItem> itemSet] => this.itemSetToState[itemSet];

    public LrItemSet<TItem> this[LrState state] => this.stateToItemSet[state];

    private readonly Dictionary<LrItemSet<TItem>, LrState> itemSetToState = new();
    private readonly Dictionary<LrState, LrItemSet<TItem>> stateToItemSet = new();

    public bool Allocate(LrItemSet<TItem> itemSet, out LrState state)
    {
        if (this.itemSetToState.TryGetValue(itemSet, out state)) return false;
        state = new(this.itemSetToState.Count);
        this.itemSetToState.Add(itemSet, state);
        this.stateToItemSet.Add(state, itemSet);
        return true;
    }

    public IEnumerator<LrStateItemSet<TItem>> GetEnumerator() =>
        this.stateToItemSet.Select(kv => new LrStateItemSet<TItem>(kv.Key, kv.Value)).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}
