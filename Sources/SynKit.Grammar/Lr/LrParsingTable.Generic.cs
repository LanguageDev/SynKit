using SynKit.Grammar.Cfg;
using SynKit.Grammar.Internal;
using SynKit.Grammar.Lr.Items;

namespace SynKit.Grammar.Lr;

/// <summary>
/// Represents a table for an LR parser.
/// </summary>
/// <typeparam name="TItem">The exact LR item type.</typeparam>
public sealed class LrParsingTable<TItem> : ILrParsingTable
    where TItem : ILrItem
{
    /// <inheritdoc/>
    public IReadOnlySet<Symbol.Terminal> Terminals { get; }

    /// <inheritdoc/>
    public IReadOnlySet<Symbol.Nonterminal> Nonterminals { get; }

    /// <inheritdoc/>
    public IReadOnlyCollection<LrStateItemSet<TItem>> StateItemSets { get; }

    /// <inheritdoc/>
    IReadOnlyCollection<LrStateItemSet<ILrItem>> ILrParsingTable.StateItemSets =>
        new ReadOnlyCollectionView<LrStateItemSet<TItem>, LrStateItemSet<ILrItem>>(
            this.StateItemSets,
            // TODO: We are allocating a lot here, uselessly
            i => new(i.State, new(i.ItemSet.Cast<ILrItem>())));

    /// <inheritdoc/>
    public LrActionTable Action { get; }

    /// <inheritdoc/>
    public LrGotoTable Goto { get; }

    /// <summary>
    /// Initializes a new <see cref="LrParsingTable{TItem}"/>.
    /// </summary>
    /// <param name="terminals">The terminals in this table.</param>
    /// <param name="nonterminals">The nonterminals in this table.</param>
    /// <param name="stateItemSets">The state and item set pairs of the automaton the table is based on.</param>
    /// <param name="action">The action table.</param>
    /// <param name="goto">The goto table.</param>
    public LrParsingTable(
        IReadOnlySet<Symbol.Terminal> terminals,
        IReadOnlySet<Symbol.Nonterminal> nonterminals,
        IReadOnlyCollection<LrStateItemSet<TItem>> stateItemSets,
        LrActionTable action,
        LrGotoTable @goto)
    {
        this.Terminals = terminals;
        this.Nonterminals = nonterminals;
        this.StateItemSets = stateItemSets;
        this.Action = action;
        this.Goto = @goto;
    }
}
