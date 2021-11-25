using SynKit.Grammar.Lr.Items;

namespace SynKit.Grammar.Lr;

/// <summary>
/// A generic LR state and item set pair.
/// </summary>
/// <typeparam name="TItem">The LR item type.</typeparam>
/// <param name="State">The LR state.</param>
/// <param name="ItemSet">The corresponding LR item set.</param>
public record LrStateItemSet<TItem>(LrState State, LrItemSet<TItem> ItemSet) : ILrStateItemSet<TItem>
    where TItem : class, ILrItem
{
    /// <inheritdoc/>
    ILrItemSet<TItem> ILrStateItemSet<TItem>.ItemSet => this.ItemSet;

    /// <inheritdoc/>
    void ILrStateItemSet<TItem>.Deconstruct(out LrState lrState, out ILrItemSet<ILrItem> itemSet)
    {
        lrState = this.State;
        itemSet = this.ItemSet;
    }
}
