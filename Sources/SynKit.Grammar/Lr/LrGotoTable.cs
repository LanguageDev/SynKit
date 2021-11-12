using SynKit.Grammar.Cfg;

namespace SynKit.Grammar.Lr;

/// <summary>
/// Represents a goto table of an LR parser.
/// </summary>
public sealed class LrGotoTable
{
    private readonly Dictionary<LrState, Dictionary<Symbol.Nonterminal, LrState>> underlying = new();

    /// <summary>
    /// The state to go to on a nonterminal.
    /// </summary>
    /// <param name="from">The source state.</param>
    /// <param name="nonterminal">The nontemrinal.</param>
    /// <returns>The destination state from state <paramref name="from"/> on nonterminal
    /// <paramref name="nonterminal"/>.</returns>
    public LrState? this[LrState from, Symbol.Nonterminal nonterminal]
    {
        get => this.underlying.TryGetValue(from, out var onMap)
            && onMap.TryGetValue(nonterminal, out var to)
                ? to
                : null;
        set
        {
            if (!this.underlying.TryGetValue(from, out var on))
            {
                on = new();
                this.underlying.Add(from, on);
            }
            if (value is null) on.Remove(nonterminal);
            else on[nonterminal] = value.Value;
        }
    }
}
