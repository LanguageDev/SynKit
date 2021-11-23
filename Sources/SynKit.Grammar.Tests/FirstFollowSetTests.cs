using SynKit.Grammar.Cfg;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SynKit.Grammar.Tests;

public class FirstFollowSetTests
{
    [Theory]
    [InlineData(@"
        E -> T E'
        E' -> + T E' | ε
        T -> F T'
        T' -> * F T' | ε
        F -> ( E ) | id",
    new[] {
        "E : (, id",
        "T : (, id",
        "F : (, id",
        "E' : +, ε",
        "T' : *, ε",
    })]
    public void FirstSetTests(string grammarText, string[] firstSets)
    {
        var cfg = TestUtils.ParseCfg(grammarText);

        // Test terminal first set
        foreach (var term in cfg.Terminals)
        {
            var firstSet = cfg.FirstSet(term);
            Assert.True(firstSet.SetEquals(new[] { term }));
        }

        // Test nonterminal first set
        foreach (var firstSetText in firstSets)
        {
            var (rule, expectedTerms) = ParseSet(firstSetText);
            var firstSet = cfg.FirstSet(rule);
            Assert.True(expectedTerms.SetEquals(firstSet));
        }
    }

    [Theory]
    [InlineData(@"
        E -> T E'
        E' -> + T E' | ε
        T -> F T'
        T' -> * F T' | ε
        F -> ( E ) | id",
    new[] {
        "E : ), $",
        "E' : ), $",
        "T : +, ), $",
        "T' : +, ), $",
        "F : +, *, ), $",
    })]
    public void FollowSetTests(string grammarText, string[] followSets)
    {
        var cfg = TestUtils.ParseCfg(grammarText);

        foreach (var followSetText in followSets)
        {
            var (rule, expectedTerms) = ParseSet(followSetText);
            var followSet = cfg.FollowSet(rule);
            Assert.True(expectedTerms.SetEquals(followSet));
        }
    }

    private static (Symbol.Nonterminal Rule, IReadOnlySet<Symbol> Symbols) ParseSet(string text)
    {
        var parts = text.Split(':');
        var leftPart = parts[0].Trim();
        var rightParts = parts[1].Trim().Split(',').Select(t => t.Trim());
        var symbols = rightParts
            .Select(t => t switch
            {
                "$" => (Symbol)Symbol.Terminal.EndOfInput,
                "ε" => Symbol.Epsilon.Instance,
                _ => new Symbol.Terminal(t),
            })
            .ToHashSet();
        return (new(leftPart), symbols);
    }
}
