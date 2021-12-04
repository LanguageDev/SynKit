using SynKit.Collections;
using System.Diagnostics.CodeAnalysis;
using Token = SynKit.Text.Token<SynKit.Grammar.ContextFree.Internal.CfTokenType>;

namespace SynKit.Grammar.ContextFree.Internal;

internal sealed class CfParser
{
    private readonly record struct ProductionNode(Token Left, List<List<Token>> Rights);

    private readonly CfLexer lexer;
    private readonly RingBuffer<Token> peekBuffer = new();

    public CfParser(CfLexer lexer)
    {
        this.lexer = lexer;
    }

    public CfGrammar Parse()
    {
        var prods = new List<ProductionNode>();
        for (; !this.Matches(CfTokenType.End); prods.Add(this.ParseProduction())) ;

        // TODO: Do conversion
        throw new NotImplementedException();
    }

    private ProductionNode ParseProduction()
    {
        // TODO: Do parsing
        throw new NotImplementedException();
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
