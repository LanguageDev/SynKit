using SynKit.Grammar.Cfg;

namespace SynKit.Grammar.Ebnf.Internal;

internal sealed class EbnfToCfgConverter
{
    // NOTE: We should make this a config, really depends on the parsing method
    private const bool PreferLeftRecursion = true;

    private readonly EbnfGrammar ebnfGrammar;
    private readonly ContextFreeGrammar cfg = new();
    private int subruleCounter = 0;

    public EbnfToCfgConverter(EbnfGrammar ebnfGrammar)
    {
        this.ebnfGrammar = ebnfGrammar;
    }

    public ContextFreeGrammar ToCfg()
    {
        if (this.ebnfGrammar.StartRule is null) throw new InvalidOperationException("The starting rule of the eBNF grammar is not set.");
        this.CheckRulePresence(this.ebnfGrammar.StartRule);

        this.cfg.AddProduction(new(Symbol.Nonterminal.Start, new Symbol[] { new Symbol.Nonterminal(this.ebnfGrammar.StartRule) }));
        foreach (var (name, ast) in this.ebnfGrammar.Rules) this.ConvertRule(name, ast);
        return this.cfg;
    }

    private void ConvertRule(string name, EbnfAst ast)
    {
        var left = new Symbol.Nonterminal(name);
        foreach (var rightSeq in this.ConvertNode(name, ast))
        {
            var right = rightSeq.ToList();
            this.cfg.AddProduction(new(left, right));
        }
    }

    private IEnumerable<IEnumerable<Symbol>> ConvertNode(string name, EbnfAst node) => node switch
    {
        EbnfAst.Rule rule => this.ConvertRuleNode(rule),
        EbnfAst.Term term => ConvertTermNode(term),
        EbnfAst.Alt alt => this.ConvertAltNode(name, alt),
        EbnfAst.Seq seq => this.ConvertSeqNode(name, seq),
        EbnfAst.Opt opt => this.ConvertOptNode(name, opt),
        EbnfAst.Rep0 rep0 => this.ConvertRep0Node(name, rep0),
        EbnfAst.Rep1 rep1 => this.ConvertRep1Node(name, rep1),
        EbnfAst.RepN repn => this.ConvertRepNNode(name, repn),
        _ => throw new ArgumentOutOfRangeException(nameof(node)),
    };

    private IEnumerable<IEnumerable<Symbol>> ConvertRuleNode(EbnfAst.Rule rule)
    {
        this.CheckRulePresence(rule.Name);
        yield return new[] { new Symbol.Nonterminal(rule.Name) };
    }

    private static IEnumerable<IEnumerable<Symbol>> ConvertTermNode(EbnfAst.Term term)
    {
        yield return new[] { new Symbol.Terminal(term.Value) };
    }

    private IEnumerable<IEnumerable<Symbol>> ConvertAltNode(string name, EbnfAst.Alt alt) =>
        this.ConvertNode(name, alt.Left).Concat(this.ConvertNode(name, alt.Right));

    private IEnumerable<IEnumerable<Symbol>> ConvertSeqNode(string name, EbnfAst.Seq seq)
    {
        var lefts = this.ConvertNode(name, seq.Left).ToList();
        var rights = this.ConvertNode(name, seq.Right).ToList();
        foreach (var l in lefts)
        {
            foreach (var r in rights) yield return l.Concat(r);
        }
    }

    private IEnumerable<IEnumerable<Symbol>> ConvertOptNode(string name, EbnfAst.Opt opt)
    {
        foreach (var item in this.ConvertNode(name, opt.Element)) yield return item;
        yield return Array.Empty<Symbol>();
    }

    private IEnumerable<IEnumerable<Symbol>> ConvertRep0Node(string name, EbnfAst.Rep0 rep0)
    {
        var symbol = this.ConvertRepAsSubrule(name, new(rep0.Element, 0, null));
        yield return new[] { symbol };
    }

    private IEnumerable<IEnumerable<Symbol>> ConvertRep1Node(string name, EbnfAst.Rep1 rep1)
    {
        var symbol = this.ConvertRepAsSubrule(name, new(rep1.Element, 1, null));
        yield return new[] { symbol };
    }

    private IEnumerable<IEnumerable<Symbol>> ConvertRepNNode(string name, EbnfAst.RepN repn)
    {
        var symbol = this.ConvertRepAsSubrule(name, repn);
        yield return new[] { symbol };
    }

    private Symbol ConvertRepAsSubrule(string name, EbnfAst.RepN node)
    {
        var subName = $"{name}@sub{this.subruleCounter++}";
        var subSym = new Symbol.Nonterminal(subName);
        // Construct min prefix
        var minSymbols = (IEnumerable<IEnumerable<Symbol>>)new[] { Enumerable.Empty<Symbol>() };
        if (node.Min != 0)
        {
            var preNode = node.Element;
            for (var i = 1; i < node.Min; ++i) preNode = new EbnfAst.Seq(preNode, node.Element);
            minSymbols = this.ConvertNode(subName, preNode);
        }
        if (node.Max is null)
        {
            // No upper-bound, recursive
            foreach (var minSymbol in minSymbols.Select(s => s.ToList()))
            {
                // Add 0 case
                this.cfg.AddProduction(new(subSym, minSymbol));
                // Add 1 and 1+ cases
                foreach (var repSymbol in this.ConvertNode(name, node.Element).Select(s => s.ToList()))
                {
                    // 1 case
                    var right = minSymbol.Concat(repSymbol).ToList();
                    this.cfg.AddProduction(new(subSym, right));
                    // 1+ case
                    var right1 = minSymbol
                        .Concat(PreferLeftRecursion ? repSymbol.Prepend(subSym) : repSymbol.Append(subSym))
                        .ToList();
                    this.cfg.AddProduction(new(subSym, right1));
                }
            }
        }
        else
        {
            // Upper-bound, nonrecursive
            // Add exactly min case
            foreach (var minSymbol in minSymbols.Select(s => s.ToList())) this.cfg.AddProduction(new(subSym, minSymbol));
            // Add min + i, i > 0 cases by constructing a sequence of optionals
            EbnfAst? optRight = null;
            for (var i = node.Min + 1; i <= node.Max.Value; ++i)
            {
                var part = new EbnfAst.Opt(node.Element);
                optRight = optRight == null ? part : new EbnfAst.Seq(optRight, part);
            }
            // If there's at least 1 optional, append those
            if (optRight is not null)
            {
                var rightSymbols = this.ConvertNode(subName, optRight);
                foreach (var minSymbol in minSymbols.Select(s => s.ToList()))
                {
                    foreach (var rightSymbol in rightSymbols)
                    {
                        var seq = minSymbol.Concat(rightSymbol).ToList();
                        this.cfg.AddProduction(new(subSym, seq));
                    }
                }
            }
        }
        return subSym;
    }

    private void CheckRulePresence(string name)
    {
        if (this.ebnfGrammar.Rules.ContainsKey(name)) return;
        throw new InvalidOperationException($"eBNF grammar contains no rule named {name}.");
    }
}
