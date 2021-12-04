using SynKit.Collections;
using System.Diagnostics.CodeAnalysis;
using Token = SynKit.Text.Token<SynKit.Grammar.ContextFree.Internal.CfTokenType>;

namespace SynKit.Grammar.ContextFree.Internal;

internal sealed class CfParser
{
    private readonly record struct ProductionNode(Token Left, IReadOnlyList<IReadOnlyList<Token>> Rights);

    private readonly CfLexer lexer;
    private readonly RingBuffer<Token> peekBuffer = new();

    public CfParser(CfLexer lexer)
    {
        this.lexer = lexer;
    }

    public CfGrammar Parse()
    {
        // Parse all productions
        var prods = new List<ProductionNode>();
        for (; !this.Matches(CfTokenType.End); prods.Add(this.ParseProduction())) ;

        // Convert
        var cfg = new CfGrammar();
        var nontermNames = prods.Select(p => p.Left.Text).ToHashSet();
        var first = true;
        foreach (var (left, rights) in prods)
        {
            var leftNt = new Symbol.Nonterminal(left.Text);
            if (first)
            {
                // Register the first production
                cfg.AddProduction(new(Symbol.Nonterminal.Start, new[] { leftNt }));
                first = false;
            }
            foreach (var right in rights)
            {
                var rightSyms = right
                    .Select(t => nontermNames.Contains(t.Text)
                        ? (Symbol)new Symbol.Nonterminal(t.Text)
                        : new Symbol.Terminal(t.Text))
                    .ToList();
                cfg.AddProduction(new(leftNt, rightSyms));
            }
        }
        return cfg;
    }

    private ProductionNode ParseProduction()
    {
        var left = this.Expect(CfTokenType.Name);
        this.Expect(CfTokenType.Arrow);
        var rights = this.ParseProductionRights();
        return new(left, rights);
    }

    private IReadOnlyList<IReadOnlyList<Token>> ParseProductionRights()
    {
        var result = new List<IReadOnlyList<Token>>();
        // Skip the first optional '|'
        this.Matches(CfTokenType.Or);
        var first = this.ParseProductionRight();
        result.Add(first);
        // While there's an '|' separator, parse another alternative
        for (; this.Matches(CfTokenType.Or); result.Add(this.ParseProductionRight())) ;
        return result;
    }

    private IReadOnlyList<Token> ParseProductionRight()
    {
        // Epsilon can be a singleton on the right
        if (this.Matches(CfTokenType.Epsilon, out var t)) return new[] { t };
        // Otherwise we expect 1..N elements
        var elements = new List<Token>();
        while (true)
        {
            var element = this.TryParseProductionElement();
            if (element is null) break;
            elements.Add(element);
        }
        // TODO: Proper error handling
        if (elements.Count == 0) throw new NotImplementedException("Expected at least one element");
        return elements;
    }

    private Token? TryParseProductionElement()
    {
        var peek = this.Peek();
        // We eat literals conditionless
        if (peek.Kind == CfTokenType.Literal) return this.Consume();
        // Only eat names if they aren't a prefix for another production
        if (peek.Kind == CfTokenType.Name && this.Peek(1).Kind != CfTokenType.Arrow) return this.Consume();
        // No luck
        return null;
    }

    private Token Expect(CfTokenType type)
    {
        // TODO: Proper error handling
        if (!this.Matches(type, out var token)) throw new NotImplementedException($"Expected {type}");
        return token;
    }

    private Token Peek(int i = 0)
    {
        for (; this.peekBuffer.Count <= i; this.peekBuffer.AddBack(this.lexer.Next())) ;
        return this.peekBuffer[i];
    }

    private Token Consume()
    {
        this.Peek();
        return this.peekBuffer.RemoveFront();
    }

    private bool Matches(CfTokenType type) => this.Matches(type, out _);

    private bool Matches(CfTokenType type, [MaybeNullWhen(false)] out Token token)
    {
        if (this.Peek().Kind == type)
        {
            token = this.Consume();
            return true;
        }
        else
        {
            token = default;
            return false;
        }
    }
}
