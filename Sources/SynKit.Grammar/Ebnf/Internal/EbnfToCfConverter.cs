using SynKit.Grammar.ContextFree;
using System.Diagnostics;

namespace SynKit.Grammar.Ebnf.Internal;

internal sealed class EbnfToCfConverter
{
    // NOTE: We should make this a config, really depends on the parsing method
    private const bool PreferLeftRecursion = true;

    private readonly EbnfGrammar ebnfGrammar;
    private readonly CfGrammar cfg = new();
    private readonly HashSet<string> validRules = new();
    private int subruleCounter = 0;

    public EbnfToCfConverter(EbnfGrammar ebnfGrammar)
    {
        this.ebnfGrammar = ebnfGrammar;
    }

    public CfGrammar ToCf()
    {
        if (this.ebnfGrammar.StartRule is null) throw new InvalidOperationException("The starting rule of the eBNF grammar is not set.");

        this.RegisterAllRules();
        this.CheckRulePresence(this.ebnfGrammar.StartRule);

        this.cfg.AddProduction(new(Symbol.Nonterminal.Start, new Symbol[] { new Symbol.Nonterminal(this.ebnfGrammar.StartRule) }));
        foreach (var (name, ast) in this.ebnfGrammar.Rules) this.ConvertRule(name, ast);
        return this.cfg;
    }

    private void ConvertRule(string name, EbnfAst ast)
    {
        ast = ast.Normalize();
        var left = new Symbol.Nonterminal(name);
        foreach (var rightSeq in this.ConvertAltLevel(name, ast))
        {
            var right = rightSeq.ToList();
            this.cfg.AddProduction(new(left, right));
        }
    }

    private IEnumerable<IEnumerable<Symbol>> ConvertAltLevel(string name, EbnfAst node)
    {
        if (node is not EbnfAst.Alt alt) return new[] { this.ConvertSeqLevel(name, node) };
        return this.ConvertAltLevel(name, alt.Left).Concat(this.ConvertAltLevel(name, alt.Right));
    }

    private IEnumerable<Symbol> ConvertSeqLevel(string name, EbnfAst node)
    {
        if (node is not EbnfAst.Seq seq) return this.ConvertAtomLevel(name, node);
        return this.ConvertSeqLevel(name, seq.Left).Concat(this.ConvertSeqLevel(name, seq.Right));
    }

    private IEnumerable<Symbol> ConvertAtomLevel(string name, EbnfAst node)
    {
        if (node is EbnfAst.Epsilon) return Enumerable.Empty<Symbol>();
        if (node is EbnfAst.Term term) return new[] { new Symbol.Terminal(term.Value) };
        if (node is EbnfAst.Rule rule)
        {
            this.CheckRulePresence(rule.Name);
            return new[] { new Symbol.Nonterminal(rule.Name) };
        }
        if (node is EbnfAst.Rep rep)
        {
            Debug.Assert(rep.Min == 0);
            Debug.Assert(rep.Max is null);
            // Make a sub-rule in the form
            // Sub -> Sub Element | Epsilon
            // or
            // Sub -> Element Sub | Epsilon
            // Depending on which recursion we prefer
            var sub = this.MakeSubruleName(name);
            this.validRules.Add(sub);
            var recAst = PreferLeftRecursion
                ? new EbnfAst.Seq(new EbnfAst.Rule(sub), rep.Element)
                : new EbnfAst.Seq(rep.Element, new EbnfAst.Rule(sub));
            this.ConvertRule(sub, new EbnfAst.Alt(recAst, EbnfAst.Epsilon.Instance));
            return new[] { new Symbol.Nonterminal(sub) };
        }
        throw new ArgumentOutOfRangeException(nameof(node));
    }

    private string MakeSubruleName(string name) => $"{name}@sub{this.subruleCounter++}";

    private void RegisterAllRules()
    {
        foreach (var rule in this.ebnfGrammar.Rules.Keys) this.validRules.Add(rule);
    }

    private void CheckRulePresence(string name)
    {
        if (this.ebnfGrammar.Rules.ContainsKey(name)) return;
        throw new InvalidOperationException($"eBNF grammar contains no rule named {name}.");
    }
}
