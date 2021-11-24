using SynKit.Grammar.Lr.Items;

namespace SynKit.Grammar.Lr;

/// <summary>
/// A generic LR state and item set pair.
/// </summary>
/// <typeparam name="TItem">The LR item type.</typeparam>
/// <param name="State">The LR state.</param>
/// <param name="ItemSet">The corresponding LR item set.</param>
public record struct LrStateItemSet<TItem>(LrState State, LrItemSet<TItem> ItemSet)
    where TItem : ILrItem;
