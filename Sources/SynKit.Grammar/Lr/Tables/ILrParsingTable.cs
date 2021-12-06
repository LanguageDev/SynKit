using SynKit.Grammar.ContextFree;
using SynKit.Grammar.Lr.Items;

namespace SynKit.Grammar.Lr.Tables;

/// <summary>
/// An LR parsing table that contains the action and goto tables.
/// </summary>
public interface ILrParsingTable
{
    /// <summary>
    /// The terminals that can be found in this table.
    /// </summary>
    public IReadOnlySet<Symbol.Terminal> Terminals { get; }

    /// <summary>
    /// The nonterminals that can be found in this table.
    /// </summary>
    public IReadOnlySet<Symbol.Nonterminal> Nonterminals { get; }

    /// <summary>
    /// The states and their associated item sets in the table.
    /// </summary>
    public IReadOnlyCollection<ILrStateItemSet<ILrItem>> StateItemSets { get; }

    /// <summary>
    /// The states in the table.
    /// </summary>
    public IReadOnlyCollection<LrState> States { get; }

    /// <summary>
    /// The LR action table.
    /// </summary>
    public LrActionTable Action { get; }

    /// <summary>
    /// The LR goto table.
    /// </summary>
    public LrGotoTable Goto { get; }
}
