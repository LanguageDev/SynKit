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
}
