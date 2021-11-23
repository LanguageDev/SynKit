using SynKit.Grammar.Cfg;

namespace SynKit.Grammar.Lr.Items;

/// <summary>
/// An LALR item.
/// </summary>
public sealed record LalrItem(Production Production, int Cursor, IReadOnlySet<Symbol.Terminal> Lookaheads)
    : LrItemBase<LalrItem>(Production, Cursor)
{
    /// <inheritdoc/>
    protected override LalrItem MakeWithCursor(int cursor) => new(this.Production, cursor, this.Lookaheads);

    /// <inheritdoc/>
    public bool Equals(LalrItem? other) =>
           other is not null
        && this.Production.Equals(other.Production)
        && this.Cursor == other.Cursor
        && this.Lookaheads.SetEquals(other.Lookaheads);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var h = default(HashCode);
        h.Add(this.Production);
        h.Add(this.Cursor);
        // We build an order-independent hash code for the lookahead set
        var setHash = 0;
        foreach (var item in this.Lookaheads) setHash ^= item.GetHashCode();
        h.Add(setHash);
        return h.ToHashCode();
    }

    /// <inheritdoc/>
    public override string ToString() =>
        $"{base.ToString()}, {(this.Lookaheads.Count == 0 ? "ε" : string.Join(" / ", this.Lookaheads))}";
}
