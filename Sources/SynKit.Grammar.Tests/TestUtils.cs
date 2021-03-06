using SynKit.Grammar.ContextFree;
using SynKit.Grammar.Lr.Items;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SynKit.Grammar.Tests;

internal class TestUtils
{
    public static CfGrammar ParseCfg(string text)
    {
        var result = new CfGrammar();
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
            .ToList();
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
        result.AddProduction(new(Symbol.Nonterminal.Start, new Symbol[] { new Symbol.Nonterminal(ruleNames[0]) }));
        return result;
    }

    public static Production ParseProduction(CfGrammar cfg, string text)
    {
        var parts = text.Split("->");
        var leftPart = parts[0].Trim();
        var left = leftPart == "START" ? Symbol.Nonterminal.Start : new Symbol.Nonterminal(parts[0].Trim());
        var rightParts = parts[1].Trim().Split(" ").Select(p => p.Trim());
        var right = new List<Symbol>();
        foreach (var part in rightParts)
        {
            right.Add(cfg.Nonterminals.Contains(new(part)) ? new Symbol.Nonterminal(part) : new Symbol.Terminal(part));
        }
        return new(left, right);
    }

    public static Lr0Item ParseLr0Item(CfGrammar cfg, string text)
    {
        var fakeProd = ParseProduction(cfg, text);
        var cursor = fakeProd.Right
            .Select((r, i) => (Symbol: r, Index: i))
            .Where(p => p.Symbol is Symbol.Terminal t && t.Value.Equals("_"))
            .Select(p => p.Index)
            .First();
        var right = fakeProd.Right.ToList();
        right.RemoveAt(cursor);
        return new(new(fakeProd.Left, right), cursor);
    }

    public static LalrItem ParseLalrItem(CfGrammar cfg, string text)
    {
        var parts = text.Split(", ");
        var lr0 = ParseLr0Item(cfg, parts[0]);
        var lookaheads = parts[1].Split("/").Select(t => t.Trim() switch
        {
            "$" => Symbol.Terminal.EndOfInput,
            string s => new Symbol.Terminal(s),
        });
        return new(lr0.Production, lr0.Cursor, lookaheads.ToHashSet());
    }

    public static ClrItem ParseClrItem(CfGrammar cfg, string text)
    {
        var lalr = ParseLalrItem(cfg, text);
        Debug.Assert(lalr.Lookaheads.Count == 1, "LR(1) items should have exactly one lookahead");
        return new(lalr.Production, lalr.Cursor, lalr.Lookaheads.First());
    }
}
