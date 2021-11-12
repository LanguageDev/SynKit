namespace SynKit.Grammar.Lr;

/// <summary>
/// A simple LR state allocator based on item sets.
/// </summary>
/// <typeparam name="TItem">The LR item type.</typeparam>
public class LrStateAllocator<TItem>
    where TItem : ILrItem
{
    /// <summary>
    /// The states allocated in the allocator.
    /// </summary>
    public IReadOnlyCollection<LrState> States => this.stateToItemSet.Keys;

    /// <summary>
    /// The item sets allocated in the allocator.
    /// </summary>
    public IReadOnlyCollection<LrItemSet<TItem>> ItemSets => this.itemSetToState.Keys;

    /// <summary>
    /// Retrieves the allocated state for a given <paramref name="itemSet"/>.
    /// </summary>
    /// <param name="itemSet">The item set to get the allocated state for.</param>
    /// <returns>The allocated state of <paramref name="itemSet"/>.</returns>
    public LrState this[LrItemSet<TItem> itemSet] => this.itemSetToState[itemSet];

    /// <summary>
    /// Retrieves the item set for a given allocated <paramref name="state"/>.
    /// </summary>
    /// <param name="state">The state to get the item set for.</param>
    /// <returns>The item set associated to <paramref name="state"/>.</returns>
    public LrItemSet<TItem> this[LrState state] => this.stateToItemSet[state];

    private readonly Dictionary<LrItemSet<TItem>, LrState> itemSetToState = new();
    private readonly Dictionary<LrState, LrItemSet<TItem>> stateToItemSet = new();

    /// <summary>
    /// Allocates a state for the given item set.
    /// </summary>
    /// <param name="itemSet">The item set to allocate a state for.</param>
    /// <param name="state">The state gets written here, that corresponds to <paramref name="itemSet"/>.</param>
    /// <returns>True, if the state was new, false otherwise.</returns>
    public bool Allocate(LrItemSet<TItem> itemSet, out LrState state)
    {
        if (this.itemSetToState.TryGetValue(itemSet, out state)) return false;
        state = new(this.itemSetToState.Count);
        this.itemSetToState.Add(itemSet, state);
        this.stateToItemSet.Add(state, itemSet);
        return true;
    }
}
