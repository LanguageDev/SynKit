using SynKit.Text;
using System.Diagnostics;

namespace SynKit.Grammar.Ebnf.Internal;

internal sealed class EbnfLexer
{
    private readonly Scanner scanner;

    public EbnfLexer(TextReader textReader)
    {
        this.scanner = new(textReader);
    }

    public Token<EbnfTokenType> Next()
    {
    start:
        // End of input
        if (!this.scanner.TryPeek(out var ch)) return this.Take(EbnfTokenType.End, 0);

        // Spaces
        if (char.IsWhiteSpace(ch) || char.IsControl(ch))
        {
            this.scanner.Consume(1);
            goto start;
        }

        // Single-character
        if (ch == '|') return this.Take(EbnfTokenType.Or, 1);
        if (ch == ',') return this.Take(EbnfTokenType.Comma, 1);
        if (ch == '?') return this.Take(EbnfTokenType.QuestionMark, 1);
        if (ch == '*') return this.Take(EbnfTokenType.Star, 1);
        if (ch == '+') return this.Take(EbnfTokenType.Plus, 1);
        if (ch == ';') return this.Take(EbnfTokenType.Semicolon, 1);
        if (ch == '(') return this.Take(EbnfTokenType.OpenParen, 1);
        if (ch == ')') return this.Take(EbnfTokenType.CloseParen, 1);
        if (ch == '[') return this.Take(EbnfTokenType.OpenBracket, 1);
        if (ch == ']') return this.Take(EbnfTokenType.CloseBracket, 1);
        if (ch == '{') return this.Take(EbnfTokenType.OpenBrace, 1);
        if (ch == '}') return this.Take(EbnfTokenType.CloseBrace, 1);

        // Assignments
        if (ch == '=') return this.Take(EbnfTokenType.Assign, 1);
        if (this.scanner.Matches("::=")) return this.Take(EbnfTokenType.Assign, 3);
        if (ch == ':') return this.Take(EbnfTokenType.Assign, 1);

        // Literal string
        if (ch == '"' || ch == '\'' || ch == '‘' || ch == '“')
        {
            var close = ch switch
            {
                '‘' => '’',
                '“' => '”',
                _ => ch,
            };
            var i = 1;
            while (true)
            {
                // TODO: Proper error handling
                if (!this.scanner.TryPeek(i++, out ch)) throw new NotImplementedException("uncolsed literal");
                if (ch == close) break;
                // TODO: Proper error handling
                if (ch == '\\' && !this.scanner.TryPeek(i++, out _)) throw new NotImplementedException("no escape");
            }
            return this.Take(EbnfTokenType.Literal, i);
        }

        // Number
        if (char.IsDigit(ch))
        {
            // Consume name characters
            var i = 1;
            for (; this.scanner.TryPeek(i, out ch) && char.IsDigit(ch); ++i) ;
            return this.Take(EbnfTokenType.Number, i);
        }

        // Name
        if (IsName(ch))
        {
            // Consume name characters
            var i = 1;
            for (; this.scanner.TryPeek(i, out ch) && IsName(ch); ++i) ;
            return this.Take(EbnfTokenType.Name, i);
        }

        // TODO: Proper error handling
        throw new NotImplementedException($"unexpected character {ch}");
    }

    private Token<EbnfTokenType> Take(EbnfTokenType type, int length)
    {
        var result = this.scanner.Consume(length, (range, str) => new Token<EbnfTokenType>(range, str, type));
        Debug.Assert(result is not null);
        return result;
    }

    private static bool IsName(char ch) => char.IsLetterOrDigit(ch) || ch == '_';
}
