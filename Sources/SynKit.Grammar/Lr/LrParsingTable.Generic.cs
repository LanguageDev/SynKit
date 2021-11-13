using SynKit.Grammar.Cfg;

namespace SynKit.Grammar.Lr;

/// <summary>
/// Represents a table for an LR parser.
/// </summary>
/// <typeparam name="TItem">The exact LR item type.</typeparam>
public class LrParsingTable<TItem> : ILrParsingTable
    where TItem : ILrItem
{
    /// <summary>
    /// The grammar the table was generated for.
    /// </summary>
    public ContextFreeGrammar Grammar { get; }

    /// <summary>
    /// The LR state allocator.
    /// </summary>
    public LrStateAllocator<TItem> StateAllocator { get; }

    /// <inheritdoc/>
    public LrActionTable Action { get; }

    /// <inheritdoc/>
    public LrGotoTable Goto { get; }

    /// <summary>
    /// Initializes a new <see cref="LrParsingTable{TItem}"/>.
    /// </summary>
    /// <param name="grammar">The grammar the table is constructed for.</param>
    /// <param name="stateAllocator">The state allocator the construction used.</param>
    /// <param name="action">The action table.</param>
    /// <param name="goto">The goto table.</param>
    public LrParsingTable(
        ContextFreeGrammar grammar,
        LrStateAllocator<TItem> stateAllocator,
        LrActionTable action,
        LrGotoTable @goto)
    {
        this.Grammar = grammar;
        this.StateAllocator = stateAllocator;
        this.Action = action;
        this.Goto = @goto;
    }
}
