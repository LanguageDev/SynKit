using SynKit.Grammar.Cfg;
using SynKit.Grammar.Lr;
using SynKit.Grammar.Lr.Internal;
using System.Linq;
using Xunit;

namespace SynKit.Grammar.Tests;

public class CounterexampleTests
{
    private static readonly Symbol.Nonterminal stmt = new("stmt");
    private static readonly Symbol.Nonterminal expr = new("expr");
    private static readonly Symbol.Nonterminal num = new("num");
    private static readonly Symbol.Terminal T_if = new("if");
    private static readonly Symbol.Terminal T_then = new("then");
    private static readonly Symbol.Terminal T_else = new("else");
    private static readonly Symbol.Terminal T_q = new("?");
    private static readonly Symbol.Terminal T_obracked = new("[");
    private static readonly Symbol.Terminal T_cbracked = new("]");
    private static readonly Symbol.Terminal T_asgn = new(":=");
    private static readonly Symbol.Terminal T_plus = new("+");
    private static readonly Symbol.Terminal T_digit = new("digit");
    private static readonly Symbol.Terminal T_arr = new("arr");

    [Fact]
    public void ShortestLookaheadPathForDanglingElse()
    {
        var table = CreateTestTable();
        var T_else = new Symbol.Terminal("else");

        var conflict = table.Action.ConflictingTransitions.First(t => t.Terminal.Equals(T_else));
        var conflictItem = table.StateAllocator[conflict.State].Items.First(i => i.IsFinal);
        var conflictItem2 = table.StateAllocator[conflict.State].Items.First(i => !i.IsFinal);

        var pathSearch = new LookaheadPath<LalrItem>(table);
        var path = pathSearch.Search(conflict.State, conflictItem, T_else);

        Assert.Equal(10, path.Count);
        // State 0
        Assert.Equal(new(Prod(table.Grammar.StartSymbol!, stmt), 0), path[0].Item);
        Assert.True(path[0].Lookaheads.SetEquals(new[] { Symbol.Terminal.EndOfInput }));
        // State 1
        Assert.Equal(new(Prod(stmt, T_if, expr, T_then, stmt, T_else, stmt), 0), path[1].Item);
        Assert.True(path[1].Lookaheads.SetEquals(new[] { Symbol.Terminal.EndOfInput }));
        // State 2
        Assert.Equal(new(Prod(stmt, T_if, expr, T_then, stmt, T_else, stmt), 1), path[2].Item);
        Assert.True(path[2].Lookaheads.SetEquals(new[] { Symbol.Terminal.EndOfInput }));
        // State 3
        Assert.Equal(new(Prod(stmt, T_if, expr, T_then, stmt, T_else, stmt), 2), path[3].Item);
        Assert.True(path[3].Lookaheads.SetEquals(new[] { Symbol.Terminal.EndOfInput }));
        // State 4
        Assert.Equal(new(Prod(stmt, T_if, expr, T_then, stmt, T_else, stmt), 3), path[4].Item);
        Assert.True(path[4].Lookaheads.SetEquals(new[] { Symbol.Terminal.EndOfInput }));
        // State 5
        Assert.Equal(new(Prod(stmt, T_if, expr, T_then, stmt), 0), path[5].Item);
        Assert.True(path[5].Lookaheads.SetEquals(new[] { T_else }));
        // State 6
        Assert.Equal(new(Prod(stmt, T_if, expr, T_then, stmt), 1), path[6].Item);
        Assert.True(path[6].Lookaheads.SetEquals(new[] { T_else }));
        // State 7
        Assert.Equal(new(Prod(stmt, T_if, expr, T_then, stmt), 2), path[7].Item);
        Assert.True(path[7].Lookaheads.SetEquals(new[] { T_else }));
        // State 8
        Assert.Equal(new(Prod(stmt, T_if, expr, T_then, stmt), 3), path[8].Item);
        Assert.True(path[8].Lookaheads.SetEquals(new[] { T_else }));
        // State 9
        Assert.Equal(new(Prod(stmt, T_if, expr, T_then, stmt), 4), path[9].Item);
        Assert.True(path[9].Lookaheads.SetEquals(new[] { T_else }));
    }

    private static LrParsingTable<LalrItem> CreateTestTable()
    {
        var cfg = new ContextFreeGrammar();
        cfg.StartSymbol = new("stmt");
        cfg.AddProduction(Prod(stmt, T_if, expr, T_then, stmt, T_else, stmt));
        cfg.AddProduction(Prod(stmt, T_if, expr, T_then, stmt));
        cfg.AddProduction(Prod(stmt, expr, T_q, stmt, stmt));
        cfg.AddProduction(Prod(stmt, T_arr, T_obracked, expr, T_cbracked, T_asgn, expr));
        cfg.AddProduction(Prod(expr, num));
        cfg.AddProduction(Prod(expr, expr, T_plus, expr));
        cfg.AddProduction(Prod(num, T_digit));
        cfg.AddProduction(Prod(num, num, T_digit));
        cfg.AugmentStartSymbol();

        return LrParsingTable.Lalr(cfg);
    }

    private static Production Prod(Symbol.Nonterminal left, params Symbol[] right) => new(left, right);
}
