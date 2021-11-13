namespace SynKit.Grammar.Lr;

/// <summary>
/// Represents an LR state.
/// </summary>
/// <param name="Id">The state identifier.</param>
public record struct LrState(int Id)
{
    /// <summary>
    /// The state to be considered as initial.
    /// </summary>
    public static LrState Initial { get; } = new(0);
}
