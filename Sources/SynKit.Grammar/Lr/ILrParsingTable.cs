namespace SynKit.Grammar.Lr;

/// <summary>
/// An LR parsing table that just containst the action and goto tables, nothing more.
/// </summary>
public interface ILrParsingTable
{
    /// <summary>
    /// The LR action table.
    /// </summary>
    public LrActionTable Action { get; }

    /// <summary>
    /// The LR goto table.
    /// </summary>
    public LrGotoTable Goto { get; }
}
