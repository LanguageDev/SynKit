using SynKit.Grammar.ContextFree;

namespace SynKit.Grammar.Lr.Tables;

/// <summary>
/// Represents a goto table of an LR parser.
/// </summary>
public sealed class LrGotoTable
{
    private readonly Dictionary<(LrState, Symbol.Nonterminal), LrState> underlying = new();

    /// <summary>
    /// The state to go to on a nonterminal.
    /// </summary>
    /// <param name="from">The source state.</param>
    /// <param name="nonterminal">The nontemrinal.</param>
    /// <returns>The destination state from state <paramref name="from"/> on nonterminal
    /// <paramref name="nonterminal"/>.</returns>
    public LrState? this[LrState from, Symbol.Nonterminal nonterminal]
    {
        get => this.underlying.TryGetValue((from, nonterminal), out var to)
            ? to
            : null;
        set
        {
            if (value is null) this.underlying.Remove((from, nonterminal));
            else this.underlying[(from, nonterminal)] = value.Value;
        }
    }
}
