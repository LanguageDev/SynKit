using SynKit.Grammar.Cfg;
using System.Collections.Generic;
using System.Linq;

namespace SynKit.Grammar.Tests;

internal class TestUtils
{
    public static ContextFreeGrammar ParseCfg(string text)
    {
        var result = new ContextFreeGrammar();
        var tokens = text
            .Split(' ', '\r', '\n')
            .Select(t => t.Trim())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToList();
        var arrowPositions = tokens
            .Select((t, i) => (Token: t, Index: i))
            .Where(p => p.Token == "->")
            .Select(p => p.Index)
            .ToList();
        var ruleNames = arrowPositions
            .Select(pos => tokens[pos - 1])
            .ToHashSet();
        for (var i = 0; i < arrowPositions.Count; ++i)
        {
            var arrowPosition = arrowPositions[i];
            var productionName = tokens[arrowPosition - 1];
            var productionsUntil = tokens.Count;
            if (i < arrowPositions.Count - 1) productionsUntil = arrowPositions[i + 1] - 1;
            var productions = tokens.GetRange(arrowPosition + 1, productionsUntil - (arrowPosition + 1));
            while (productions.Count > 0)
            {
                var end = productions.IndexOf("|");
                if (end == -1) end = productions.Count;
                else productions.RemoveAt(end);
                var prodSymbols = new List<Symbol>();
                if (productions[0] != "ε")
                {
                    prodSymbols = productions
                        .Take(end)
                        .Select(t => ruleNames.Contains(t)
                            ? (Symbol)new Symbol.Nonterminal(t)
                            : new Symbol.Terminal(t))
                        .ToList();
                }
                result.AddProduction(new(new(productionName), prodSymbols));
                productions.RemoveRange(0, end);
            }
        }
        return result;
    }
}
