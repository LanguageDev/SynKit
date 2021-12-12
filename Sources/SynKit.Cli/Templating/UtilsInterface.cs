using System.Collections;

namespace SynKit.Cli.Templating;

/// <summary>
/// General utilities in Scriban templates.
/// </summary>
public static class UtilsInterface
{
    /// <summary>
    /// Counts the number of elements in an enumerable.
    /// </summary>
    /// <param name="items">The enumerable to count the number of elements of.</param>
    /// <returns>The number of elements in <paramref name="items"/>.</returns>
    public static int Count(IEnumerable items)
    {
        var count = 0;
        foreach (var _ in items) ++count;
        return count;
    }

    /// <summary>
    /// Checks, if a given enumerable is empty.
    /// </summary>
    /// <param name="items">The enumerable to check.</param>
    /// <returns>True, if there are no elements in <paramref name="items"/>.</returns>
    public static bool IsEmpty(IEnumerable items) => !items.GetEnumerator().MoveNext();

    /// <summary>
    /// Checks, if a given enumerable contains a single element.
    /// </summary>
    /// <param name="items">The enumerable to check.</param>
    /// <returns>True, if <paramref name="items"/> contains a single element.</returns>
    public static bool IsSingle(IEnumerable items)
    {
        var enumerator = items.GetEnumerator();
        return enumerator.MoveNext() && !enumerator.MoveNext();
    }

    /// <summary>
    /// Retrieves the first element of an enumerable.
    /// </summary>
    /// <param name="items">The enumerable to retrieve the first element from.</param>
    /// <returns>The first element of <paramref name="items"/>, or null if it was empty.</returns>
    public static object? First(IEnumerable items)
    {
        var enumerator = items.GetEnumerator();
        return enumerator.MoveNext() ? enumerator.Current : null;
    }

    // TODO: Doc
    public static IDictionary<object, object> AssignIds(IEnumerable items)
    {
        var result = new Dictionary<object, object>();
        foreach (var item in items)
        {
            if (!result.ContainsKey(item)) result.Add(item, result.Count);
        }
        return result;
    }
}
