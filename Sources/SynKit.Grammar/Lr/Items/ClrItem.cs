using SynKit.Grammar.ContextFree;

namespace SynKit.Grammar.Lr.Items;

/// <summary>
/// A canonical LR (aka. LR(1)) item.
/// </summary>
public sealed record ClrItem(Production Production, int Cursor, Symbol.Terminal Lookahead)
    : LrItemBase<ClrItem>(Production, Cursor)
{
    /// <inheritdoc/>
    public override string ToString() => $"[{base.ToString()}, {this.Lookahead}]";

    /// <inheritdoc/>
    protected override ClrItem MakeWithCursor(int cursor) => new(this.Production, cursor, this.Lookahead);
}
