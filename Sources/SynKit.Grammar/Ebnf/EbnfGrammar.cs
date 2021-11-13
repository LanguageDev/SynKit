using SynKit.Grammar.Cfg;
using SynKit.Grammar.Ebnf.Internal;

namespace SynKit.Grammar.Ebnf;

/// <summary>
/// Represents a collection of eBNF rules to make up a grammar.
/// </summary>
public sealed class EbnfGrammar
{
    /// <summary>
    /// The rules in this grammar.
    /// </summary>
    public IDictionary<string, EbnfAst> Rules { get; } = new Dictionary<string, EbnfAst>();

    /// <summary>
    /// The starting rule name.
    /// </summary>
    public string? StartRule { get; set; }

    /// <summary>
    /// Converts this eBNF grammar to a <see cref="ContextFreeGrammar"/>.
    /// </summary>
    /// <returns>The <see cref="ContextFreeGrammar"/> recognizing the same language as this.</returns>
    public ContextFreeGrammar ToContextFreeGrammar() => new EbnfToCfgConverter(this).ToCfg();
}
