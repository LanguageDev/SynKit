namespace SynKit.Grammar.Ebnf;

/// <summary>
/// Settings for converting an EBNF grammar to a CF grammar.
/// </summary>
public sealed class EbnfToCfSettings
{
    /// <summary>
    /// Default settings.
    /// </summary>
    public static EbnfToCfSettings Default { get; } = new();

    /// <summary>
    /// When converting recursive constructs, prefer left recursion (true) or right recursion (false).
    /// </summary>
    public bool PreferLeftRecursion { get; init; } = true;
}
