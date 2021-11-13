using SynKit.Grammar.Cfg;
using System.Text;

namespace SynKit.Grammar.Lr;

/// <summary>
/// An LALR item.
/// </summary>
public sealed record LalrItem(Production Production, int Cursor, IReadOnlySet<Symbol.Terminal> Lookaheads) : ILrItem
{
    /// <inheritdoc/>
    public bool IsInitial => this.Cursor == 0;

    /// <inheritdoc/>
    public bool IsFinal => this.Cursor == this.Production.Right.Count;

    /// <inheritdoc/>
    public Symbol? AfterCursor => this.IsFinal ? null : this.Production.Right[this.Cursor];

    /// <summary>
    /// Retrieves the next item, with the cursor advanced one.
    /// </summary>
    public LalrItem Next =>
        new(this.Production, Math.Min(this.Cursor + 1, this.Production.Right.Count), this.Lookaheads);

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
    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append(this.Production.Left).Append(" ->");
        for (var i = 0; i < this.Production.Right.Count; ++i)
        {
            if (this.Cursor == i) sb.Append(" _");
            sb.Append(' ').Append(this.Production.Right[i]);
        }
        if (this.IsFinal) sb.Append(" _");
        sb.Append(", ").Append((this.Lookaheads.Count == 0 ? "ε" : string.Join("/", this.Lookaheads)));
        return sb.ToString();
    }
}
