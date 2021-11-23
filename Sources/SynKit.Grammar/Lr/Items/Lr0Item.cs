using SynKit.Grammar.Cfg;

namespace SynKit.Grammar.Lr.Items;

/// <summary>
/// An LR(0) item.
/// </summary>
public sealed record Lr0Item(Production Production, int Cursor)
    : LrItemBase<Lr0Item>(Production, Cursor)
{
    /// <inheritdoc/>
    protected override Lr0Item MakeWithCursor(int cursor) => new(this.Production, cursor);
}
