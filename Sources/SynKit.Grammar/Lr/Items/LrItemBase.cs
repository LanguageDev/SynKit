using SynKit.Grammar.ContextFree;
using System.Text;

namespace SynKit.Grammar.Lr.Items;

/// <summary>
/// A base for implementing LR items more simply.
/// </summary>
/// <typeparam name="TImpl">The exact implementation type.</typeparam>
public abstract record LrItemBase<TImpl> : ILrItem
    where TImpl : LrItemBase<TImpl>
{
    /// <inheritdoc/>
    public Production Production { get; }

    /// <inheritdoc/>
    public int Cursor { get; }

    /// <inheritdoc/>
    public bool IsInitial => this.Cursor == 0;

    /// <inheritdoc/>
    public bool IsFinal => this.Cursor == this.Production.Right.Count;

    /// <inheritdoc/>
    public bool IsKernel => !this.IsInitial || Symbol.Nonterminal.Start.Equals(this.Production.Left);

    /// <inheritdoc/>
    public Symbol? AfterCursor => this.IsFinal ? null : this.Production.Right[this.Cursor];

    /// <inheritdoc/>
    ILrItem ILrItem.Next => this.Next;

    /// <inheritdoc/>
    public TImpl Next => this.MakeWithCursor(Math.Min(this.Cursor + 1, this.Production.Right.Count));

    /// <summary>
    /// Initializes a new <see cref="LrItemBase{TImpl}"/>.
    /// </summary>
    /// <param name="production">The production of the LR item.</param>
    /// <param name="cursor">The cursor positionof the LR item.</param>
    public LrItemBase(Production production, int cursor)
    {
        if (cursor < 0 || cursor > production.Right.Count) throw new ArgumentOutOfRangeException(nameof(cursor));

        this.Production = production;
        this.Cursor = cursor;
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
        return sb.ToString();
    }

    /// <summary>
    /// Constructs the item from this one with the given cursor position.
    /// </summary>
    /// <param name="cursor">The cursor to construct the item with.</param>
    /// <returns>A new LR item with the cursor set to <paramref name="cursor"/>, the rest of the properties
    /// staying the same.</returns>
    protected abstract TImpl MakeWithCursor(int cursor);
}
