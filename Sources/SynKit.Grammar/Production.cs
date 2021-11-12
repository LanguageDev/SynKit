namespace SynKit.Grammar;

/// <summary>
/// Represents a single production rule in a context-free grammar.
/// They are in the form of Left -> Right.
/// </summary>
/// <param name="Left">The nonterminal on the left of the production.</param>
/// <param name="Right">The sequence of symbols on the right of the production.</param>
public sealed record Production(Symbol.Nonterminal Left, IReadOnlyList<Symbol> Right)
{
    /// <inheritdoc/>
    public override string ToString() =>
        $"{this.Left} -> {(this.Right.Count == 0 ? "ε" : string.Join(" ", this.Right))}";

    /// <inheritdoc/>
    public bool Equals(Production? o) =>
           o is not null
        && this.Left.Equals(o.Left)
        && this.Right.Count == o.Right.Count
        && this.Right.SequenceEqual(o.Right);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var h = default(HashCode);
        h.Add(this.Left);
        foreach (var r in this.Right) h.Add(r);
        return h.ToHashCode();
    }
}
