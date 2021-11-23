using SynKit.Grammar.Cfg;

namespace SynKit.Grammar.Lr.Items;

/// <summary>
/// Represents a generic LR item set.
/// </summary>
/// <typeparam name="TItem">The LR item type.</typeparam>
public interface ILrItemSet<out TItem> : IReadOnlyCollection<TItem>
    where TItem : ILrItem
{
    /// <summary>
    /// The kernel items in this set.
    /// </summary>
    public IEnumerable<TItem> KernelItems { get; }

    /// <summary>
    /// All items that have their cursor before a terminal.
    /// </summary>
    public IEnumerable<IGrouping<Symbol.Terminal, TItem>> ShiftItems { get; }

    /// <summary>
    /// All items that have their cursor before a nonterminal.
    /// </summary>
    public IEnumerable<IGrouping<Symbol.Nonterminal, TItem>> ProductionItems { get; }

    /// <summary>
    /// All items that have their cursor at the end.
    /// </summary>
    public IEnumerable<TItem> ReductionItems { get; }
}
