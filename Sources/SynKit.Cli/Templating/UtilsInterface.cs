using System.Collections;

namespace SynKit.Cli.Templating;

/// <summary>
/// General utilities in Scriban templates.
/// </summary>
public static class UtilsInterface
{
    /// <summary>
    /// Checks, if a given enumerable is empty.
    /// </summary>
    /// <param name="items">The enumerable to check.</param>
    /// <returns>True, if there are no elements in <paramref name="items"/>.</returns>
    public static bool IsEmpty(IEnumerable items) => !items.GetEnumerator().MoveNext();

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
}
