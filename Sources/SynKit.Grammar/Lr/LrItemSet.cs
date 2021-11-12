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
    public bool Equals(LrItemSet<TItem>? o) =>
           this.Items.Count == o.Items.Count
        && this.Items.SetEquals(o.Items);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var h = default(HashCode);
        foreach (var item in this.Items) h.Add(item);
        return h.ToHashCode();
    }
}
