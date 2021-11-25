using SynKit.Grammar.Lr.Items;

namespace SynKit.Grammar.Lr;

/// <summary>
/// Interface for a pair of LR state and item set.
/// </summary>
/// <typeparam name="TItem">The LR item type.</typeparam>
public interface ILrStateItemSet<out TItem>
    where TItem : ILrItem
{
    /// <summary>
    /// The LR state.
    /// </summary>
    public LrState State { get; }

    /// <summary>
    /// The corresponding LR item set.
    /// </summary>
    public ILrItemSet<TItem> ItemSet { get; }

    /// <summary>
    /// Deconstructs this pair of state and item set.
    /// </summary>
    /// <param name="lrState">The LR state.</param>
    /// <param name="itemSet">The corresponding LR item set.</param>
    public void Deconstruct(out LrState lrState, out ILrItemSet<ILrItem> itemSet);
}
