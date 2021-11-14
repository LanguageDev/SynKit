using SynKit.Grammar.Cfg;
using System.Text;

namespace SynKit.Grammar.Lr;

/// <summary>
/// An LR(0) item.
/// </summary>
public sealed record Lr0Item(Production Production, int Cursor) : ILrItem
{
    /// <inheritdoc/>
    public bool IsInitial => this.Cursor == 0;

    /// <inheritdoc/>
    public bool IsFinal => this.Cursor == this.Production.Right.Count;

    /// <inheritdoc/>
    public Symbol? AfterCursor => this.IsFinal ? null : this.Production.Right[this.Cursor];

    /// <inheritdoc/>
    ILrItem ILrItem.Next => this.Next;

    /// <summary>
    /// Retrieves the next item, with the cursor advanced one.
    /// </summary>
    public Lr0Item Next => new(this.Production, Math.Min(this.Cursor + 1, this.Production.Right.Count));

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
        return sb.ToString();
    }
}
