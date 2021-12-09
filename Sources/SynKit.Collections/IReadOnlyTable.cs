using System.Diagnostics.CodeAnalysis;

namespace SynKit.Collections;

/// <summary>
/// Represents a generic 2D table.
/// </summary>
/// <typeparam name="TColumn">The column index type.</typeparam>
/// <typeparam name="TRow">The row index type.</typeparam>
/// <typeparam name="TElement">The element type.</typeparam>
public interface IReadOnlyTable<TColumn, TRow, TElement>
{
    /// <summary>
    /// The column indices.
    /// </summary>
    public IReadOnlyCollection<TColumn> Columns { get; }

    /// <summary>
    /// The row indices.
    /// </summary>
    public IReadOnlyCollection<TRow> Rows { get; }

    /// <summary>
    /// Retrieves the element at the given column and row.
    /// </summary>
    /// <param name="column">The column to get the element from.</param>
    /// <param name="row">The row to get the element from.</param>
    /// <returns>The element at column <paramref name="column"/> and row <paramref name="row"/>.</returns>
    public TElement this[TColumn column, TRow row] { get; }

    /// <summary>
    /// Tries to retrieve an element at the given column and row.
    /// </summary>
    /// <param name="column">The column to get the element from.</param>
    /// <param name="row">The row to get the element from.</param>
    /// <param name="element">The retrieved element gets written here, if there was any.</param>
    /// <returns>True, if there was an element to retrieve from column <paramref name="column"/> and
    /// row <paramref name="row"/>.</returns>
    public bool TryGetElement(TColumn column, TRow row, [MaybeNullWhen(false)] out TElement element);
}
