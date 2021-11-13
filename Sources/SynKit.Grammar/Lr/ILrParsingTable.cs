using SynKit.Grammar.Cfg;

namespace SynKit.Grammar.Lr;

/// <summary>
/// An LR parsing table that just containst the action and goto tables, nothing more.
/// </summary>
public interface ILrParsingTable
{
    /// <summary>
    /// The grammar this table was generated from.
    /// </summary>
    public ContextFreeGrammar Grammar { get; }

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
