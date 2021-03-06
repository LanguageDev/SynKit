using SynKit.Grammar.ContextFree;
using SynKit.Grammar.Lr.Items;
using System.Collections;

namespace SynKit.Grammar.Lr;

/// <summary>
/// A generic LR item set implementation.
/// </summary>
/// <typeparam name="TItem">The LR item type.</typeparam>
public readonly struct LrItemSet<TItem> : ILrItemSet<TItem>, IReadOnlySet<TItem>, IEquatable<LrItemSet<TItem>>
    where TItem : ILrItem
{
    /// <inheritdoc/>
    public int Count => this.items.Count;

    /// <inheritdoc/>
    public IEnumerable<TItem> KernelItems => this.items.Where(i => i.IsKernel);

    /// <inheritdoc/>
    public IEnumerable<IGrouping<Symbol.Terminal, TItem>> ShiftItems => this.items
        .Where(prod => prod.AfterCursor is Symbol.Terminal)
        .GroupBy(prod => (Symbol.Terminal)prod.AfterCursor!);

    /// <inheritdoc/>
    public IEnumerable<IGrouping<Symbol.Nonterminal, TItem>> ProductionItems => this.items
        .Where(prod => prod.AfterCursor is Symbol.Nonterminal)
        .GroupBy(prod => (Symbol.Nonterminal)prod.AfterCursor!);

    /// <inheritdoc/>
    public IEnumerable<TItem> ReductionItems => this.items.Where(prod => prod.IsFinal);

    private readonly IReadOnlySet<TItem> items;

    /// <summary>
    /// Initializes a new <see cref="LrItemSet{TItem}"/>.
    /// </summary>
    /// <param name="items">The set of items.</param>
    public LrItemSet(IReadOnlySet<TItem> items)
    {
        this.items = items;
    }

    /// <summary>
    /// Initializes a new <see cref="LrItemSet{TItem}"/>.
    /// </summary>
    /// <param name="items">The items.</param>
    public LrItemSet(IEnumerable<TItem> items)
        : this(items.ToHashSet())
    {
    }

    /// <inheritdoc/>
    public override string ToString() => string.Join("\n", this.items);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is LrItemSet<TItem> other && this.Equals(other);

    /// <inheritdoc/>
    public bool Equals(LrItemSet<TItem> other) =>
           this.items.Count == other.items.Count
        && this.items.SetEquals(other.items);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        // NOTE: Order-independent hash
        var hashCode = 0;
        foreach (var item in this.items) hashCode ^= item.GetHashCode();
        return hashCode;
    }

    /// <inheritdoc/>
    public bool Contains(TItem item) => this.items.Contains(item);

    /// <inheritdoc/>
    public bool IsProperSubsetOf(IEnumerable<TItem> other) => this.items.IsProperSubsetOf(other);

    /// <inheritdoc/>
    public bool IsProperSupersetOf(IEnumerable<TItem> other) => this.items.IsProperSupersetOf(other);

    /// <inheritdoc/>
    public bool IsSubsetOf(IEnumerable<TItem> other) => this.items.IsSubsetOf(other);

    /// <inheritdoc/>
    public bool IsSupersetOf(IEnumerable<TItem> other) => this.items.IsSupersetOf(other);

    /// <inheritdoc/>
    public bool Overlaps(IEnumerable<TItem> other) => this.items.Overlaps(other);

    /// <inheritdoc/>
    public bool SetEquals(IEnumerable<TItem> other) => this.items.SetEquals(other);

    /// <inheritdoc/>
    public IEnumerator<TItem> GetEnumerator() => this.items.GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => this.items.GetEnumerator();

    /// <summary>
    /// Checks equality between two item sets.
    /// </summary>
    /// <param name="left">The first item set to compare.</param>
    /// <param name="right">The second item set to compare.</param>
    /// <returns>True, if <paramref name="left"/> and <paramref name="right"/> contain the same LR items.</returns>
    public static bool operator ==(LrItemSet<TItem> left, LrItemSet<TItem> right) => left.Equals(right);

    /// <summary>
    /// Checks inequality between two item sets.
    /// </summary>
    /// <param name="left">The first item set to compare.</param>
    /// <param name="right">The second item set to compare.</param>
    /// <returns>True, if <paramref name="left"/> and <paramref name="right"/> contain different LR items.</returns>
    public static bool operator !=(LrItemSet<TItem> left, LrItemSet<TItem> right) => !(left == right);
}
