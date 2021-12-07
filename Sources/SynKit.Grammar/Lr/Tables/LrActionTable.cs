using SynKit.Grammar.ContextFree;

namespace SynKit.Grammar.Lr.Tables;

/// <summary>
/// Represents the action table for an LR parser.
/// </summary>
public sealed class LrActionTable
{
    private readonly Dictionary<(LrState, Symbol.Terminal), HashSet<LrAction>> underlying = new();

    /// <summary>
    /// True, if there are conflicts in the table.
    /// </summary>
    public bool HasConflicts => this.ConflictingTransitions.Any();

    /// <summary>
    /// Retrieves the confliction transitions.
    /// </summary>
    public IEnumerable<(LrState State, Symbol.Terminal Terminal)> ConflictingTransitions => this.underlying
        .Where(kv => kv.Value.Count > 1)
        .Select(kv => kv.Key);

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
            if (!this.underlying.TryGetValue((state, terminal), out var toSet))
            {
                toSet = new();
                this.underlying.Add((state, terminal), toSet);
            }
            return toSet;
        }
    }
}
