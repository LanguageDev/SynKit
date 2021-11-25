using SynKit.Grammar.Lr.Items;

namespace SynKit.Grammar.Lr;

/// <summary>
/// Represents a set of LR items.
/// </summary>
/// <typeparam name="TItem">The LR item type.</typeparam>
public interface ILrItemSet<out TItem> : IReadOnlyCollection<TItem>
    where TItem : ILrItem
{
}
