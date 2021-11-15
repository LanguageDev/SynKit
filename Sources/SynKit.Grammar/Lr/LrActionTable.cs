using SynKit.Grammar.Cfg;

namespace SynKit.Grammar.Lr;

/// <summary>
/// Represents the action table for an LR parser.
/// </summary>
public sealed class LrActionTable
{
    private readonly Dictionary<LrState, Dictionary<Symbol.Terminal, HashSet<LrAction>>> underlying = new();

    /// <summary>
    /// True, if there are conflicts in the table.
    /// </summary>
    public bool HasConflicts => this.ConflictingTransitions.Any();

    /// <summary>
    /// Retrieves the confliction transitions.
    /// </summary>
    public IEnumerable<(LrState State, Symbol.Terminal Terminal)> ConflictingTransitions => this.underlying
        .SelectMany(kv =>
            kv.Value.Where(kv2 => kv2.Value.Count > 1)
                    .Select(kv2 => (kv.Key, kv2.Key)));

    /// <summary>
    /// Retrieves a collection of actions for a given state and terminal.
    /// </summary>
    /// <param name="state">The state.</param>
    /// <param name="terminal">The terminal.</param>
    /// <returns>The actions to perform on state <paramref name="state"/> and temrinal <paramref name="terminal"/>.</returns>
    public ICollection<LrAction> this[LrState state, Symbol.Terminal terminal]
    {
        get
        {
            if (!this.underlying.TryGetValue(state, out var onMap))
            {
                onMap = new();
                this.underlying.Add(state, onMap);
            }
            if (!onMap.TryGetValue(terminal, out var toSet))
            {
                toSet = new();
                onMap.Add(terminal, toSet);
            }
            return toSet;
        }
    }
}
