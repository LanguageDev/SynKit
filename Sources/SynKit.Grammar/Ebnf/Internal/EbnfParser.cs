using SynKit.Collections;
using System.Diagnostics.CodeAnalysis;
using Token = SynKit.Text.Token<SynKit.Grammar.Ebnf.Internal.EbnfTokenType>;

namespace SynKit.Grammar.Ebnf.Internal;

internal sealed class EbnfParser
{
    private readonly EbnfFlavor flavor;
    private readonly EbnfLexer lexer;
    private readonly RingBuffer<Token> peekBuffer = new();

    public EbnfParser(EbnfFlavor flavor, EbnfLexer lexer)
    {
        this.flavor = flavor;
        this.lexer = lexer;
    }

    public EbnfGrammar Parse()
    {
        var result = new EbnfGrammar();
        var first = true;
        while (!this.Matches(EbnfTokenType.End))
        {
            var (name, ast) = this.ParseRuleDefinition();
            if (first)
            {
                result.StartRule = name;
                first = false;
            }
            result.Rules.Add(name, ast);
        }
        return result;
    }

    private (string Name, EbnfAst Ast) ParseRuleDefinition()
    {
        var left = this.Expect(EbnfTokenType.Name);
        this.Expect(EbnfTokenType.Assign);
        var right = this.ParseAlt(true);
        // Skip optional semicolon
        this.Matches(EbnfTokenType.Semicolon);
        return new(left.Text, right);
    }

    private EbnfAst ParseAlt(bool toplevel)
    {
        // Skip the first optional '|', if toplevel
        if (toplevel) this.Matches(EbnfTokenType.Or);
        var result = this.ParseSeq(toplevel);
        // While there's an '|' separator, parse another alternative
        while (this.Matches(EbnfTokenType.Or))
        {
            var right = this.ParseSeq(toplevel);
            result = new EbnfAst.Alt(result, right);
        }
        return result;
    }

    private EbnfAst ParseSeq(bool toplevel)
    {
        EbnfAst result = EbnfAst.Epsilon.Instance;
        while (true)
        {
            var right = this.TryParsePostfix(toplevel);
            if (right is null) break;
            result = new EbnfAst.Seq(result, right);
        }
        return result;
    }

    private EbnfAst? TryParsePostfix(bool toplevel)
    {
        var result = this.TryParseAtom(toplevel);
        if (result is null) return null;
        if (this.flavor == EbnfFlavor.Regex)
        {
            // There are potential postfix operators
            while (true)
            {
                if (this.Matches(EbnfTokenType.QuestionMark)) result = EbnfAst.Opt(result);
                else if (this.Matches(EbnfTokenType.Star)) result = EbnfAst.ZeroOrMore(result);
                else if (this.Matches(EbnfTokenType.Plus)) result = EbnfAst.OneOrMore(result);
                else if (this.Matches(EbnfTokenType.OpenBrace))
                {
                    // Possibilities:
                    //  - {n, m}: n to m repetitions
                    //  - {, m}: 0 to m repetitions
                    //  - {n, }: at least n repetitions
                    //  - {n}: exactly n repetitions
                    if (this.Matches(EbnfTokenType.Number, out var lowerToken))
                    {
                        var lowerBound = int.Parse(lowerToken.Text);
                        if (this.Matches(EbnfTokenType.Comma))
                        {
                            if (this.Matches(EbnfTokenType.Number, out var upperToken))
                            {
                                this.Expect(EbnfTokenType.CloseBrace);
                                var upperBound = int.Parse(upperToken.Text);
                                result = EbnfAst.Between(result, lowerBound, upperBound);
                            }
                            else if (this.Matches(EbnfTokenType.CloseBrace))
                            {
                                result = EbnfAst.AtLeast(result, lowerBound);
                            }
                            else
                            {
                                // TODO: Proper error handling
                                throw new NotImplementedException("Unknown repetition pattern");
                            }
                        }
                        else if (this.Matches(EbnfTokenType.CloseBrace))
                        {
                            result = EbnfAst.Exactly(result, lowerBound);
                        }
                        else
                        {
                            // TODO: Proper error handling
                            throw new NotImplementedException("Unknown repetition pattern");
                        }
                    }
                    else if (this.Matches(EbnfTokenType.Comma))
                    {
                        var upperToken = this.Expect(EbnfTokenType.Number);
                        this.Expect(EbnfTokenType.CloseBrace);
                        var upperBound = int.Parse(upperToken.Text);
                        result = EbnfAst.AtMost(result, upperBound);
                    }
                    else
                    {
                        // TODO: Proper error handling
                        throw new NotImplementedException("Unknown repetition pattern");
                    }
                }
                else break;
            }
        }
        return result;
    }

    private EbnfAst? TryParseAtom(bool toplevel)
    {
        var peek = this.Peek();
        // If we are toplevel and the next 2 tokens are a new rule, this can't be an atom
        if (toplevel && peek.Kind == EbnfTokenType.Name && this.Peek(1).Kind == EbnfTokenType.Assign) return null;
        // Otherwise there might be an atom
        if (peek.Kind == EbnfTokenType.Name || peek.Kind == EbnfTokenType.Literal)
        {
            var t = this.Consume();
            return new EbnfAst.Reference(t.Text);
        }
        // Grouping is always allowed
        if (this.Matches(EbnfTokenType.OpenParen))
        {
            var sub = this.ParseAlt(false);
            this.Expect(EbnfTokenType.CloseParen);
            return sub;
        }
        // Repetition braces are only allowed, if we are using the right flavor
        if (this.flavor == EbnfFlavor.Standard)
        {
            if (this.Matches(EbnfTokenType.OpenBrace))
            {
                var sub = this.ParseAlt(false);
                this.Expect(EbnfTokenType.CloseBrace);
                return EbnfAst.ZeroOrMore(sub);
            }
            if (this.Matches(EbnfTokenType.OpenBracket))
            {
                var sub = this.ParseAlt(false);
                this.Expect(EbnfTokenType.CloseBracket);
                return EbnfAst.Opt(sub);
            }
        }
        return null;
    }

    private Token Expect(EbnfTokenType type)
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

    private bool Matches(EbnfTokenType type) => this.Matches(type, out _);

    private bool Matches(EbnfTokenType type, [MaybeNullWhen(false)] out Token token)
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
