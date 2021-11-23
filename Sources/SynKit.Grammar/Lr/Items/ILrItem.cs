using SynKit.Grammar.Cfg;

namespace SynKit.Grammar.Lr.Items;

/// <summary>
/// A generic LR(k) item interface.
/// </summary>
public interface ILrItem
{
    /// <summary>
    /// The production this item references.
    /// </summary>
    public Production Production { get; }

    /// <summary>
    /// The cursor index standing in the production.
    /// </summary>
    public int Cursor { get; }

    /// <summary>
    /// True, if this is an initial item, meaning the cursor is at the start.
    /// </summary>
    public bool IsInitial { get; }

    /// <summary>
    /// True, if this is a final item, meaning the cursor is at the end.
    /// </summary>
    public bool IsFinal { get; }

    /// <summary>
    /// True, if this is a kernel item.
    /// </summary>
    public bool IsKernel { get; }

    /// <summary>
    /// The symbol after the cursor. Null, if the cursor is at the end.
    /// </summary>
    public Symbol? AfterCursor { get; }

    /// <summary>
    /// Retrieves the next item, with the cursor advanced one.
    /// </summary>
    public ILrItem Next { get; }
}
