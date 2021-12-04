using SynKit.Text;
using System.Diagnostics;

namespace SynKit.Grammar.ContextFree.Internal;

internal sealed class CfLexer
{
    private readonly Scanner scanner;

    public CfLexer(TextReader textReader)
    {
        this.scanner = new(textReader);
    }

    public Token<CfTokenType> Next()
    {
    start:
        // End of input
        if (!this.scanner.TryPeek(out var ch)) return this.Take(CfTokenType.End, 0);

        // Spaces
        if (char.IsWhiteSpace(ch) || char.IsControl(ch))
        {
            this.scanner.Consume(1);
            goto start;
        }

        // Or
        if (ch == '|') return this.Take(CfTokenType.Or, 1);

        // Epsilon
        if (ch == 'ε') return this.Take(CfTokenType.Epsilon, 1);

        // Arrow
        if (ch == '→') return this.Take(CfTokenType.Arrow, 1);
        if (this.scanner.Matches("->")) return this.Take(CfTokenType.Arrow, 2);

        // Literal string
        if (ch == '"')
        {
            var i = 1;
            while (true)
            {
                // TODO: Proper error handling
                if (!this.scanner.TryPeek(i++, out ch)) throw new NotImplementedException("uncolsed literal");
                if (ch == '"') break;
                // TODO: Proper error handling
                if (ch == '\\' && !this.scanner.TryPeek(i++, out _)) throw new NotImplementedException("no escape");
            }
            return this.Take(CfTokenType.Literal, i);
        }

        // Name
        if (IsName(ch))
        {
            // Consume name characters
            var i = 1;
            for (; this.scanner.TryPeek(i, out ch) && IsName(ch); ++i) ;
            var result = this.Take(CfTokenType.Name, i);

            // Special cases, we can refer to epsilon as identifiers to make it less painful to type
            if (result.Text == "''" || result.Text == "eps") return new(result.Range, result.Text, CfTokenType.Epsilon);
            else return result;
        }

        // Anything else is handled as a single-character name
        return this.Take(CfTokenType.Name, 1);
    }

    private Token<CfTokenType> Take(CfTokenType type, int length)
    {
        var result = this.scanner.Consume(length, (range, str) => new Token<CfTokenType>(range, str, type));
        Debug.Assert(result is not null);
        return result;
    }

    private static bool IsName(char ch) =>
           char.IsLetterOrDigit(ch)
        || "-_:'@$<>".Contains(ch);
}
