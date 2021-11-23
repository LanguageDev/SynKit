using SynKit.Grammar.Lr.Items;

namespace SynKit.Grammar.Lr;

/// <summary>
/// Represents a generic LR item set.
/// </summary>
/// <typeparam name="TItem">The exact LR item type.</typeparam>
/// <param name="Items">The LR items in this item set.</param>
public sealed record LrItemSet<TItem>(IReadOnlySet<TItem> Items)
    where TItem : ILrItem
{
    /// <inheritdoc/>
    public override string ToString() => string.Join("\n", this.Items);

    /// <inheritdoc/>
    public bool Equals(LrItemSet<TItem>? o) =>
           o is not null
        && this.Items.Count == o.Items.Count
        && this.Items.SetEquals(o.Items);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        // NOTE: Order-independent hash
        var hashCode = 0;
        foreach (var item in this.Items) hashCode ^= item.GetHashCode();
        return hashCode;
    }
}
