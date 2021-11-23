using SynKit.Grammar.Lr;
using SynKit.Grammar.Lr.Items;
using Xunit;

namespace SynKit.Grammar.Tests;

public class LalrParsingTableTests : LrTestBase<LalrItem>
{
    [Fact]
    public void FromLr0Grammar()
    {
        var grammar = TestUtils.ParseCfg(Lr0Grammar);
        var table = LrParsingTable.Lalr(grammar);

        // Assert state count
        Assert.Equal(8, table.StateAllocator.States.Count);

        // Assert item sets
        AssertState(
            table,
            out var i0,
            "START -> _ S, $");
        AssertState(
            table,
            out var i1,
            "S -> a _ S b, b/c/$",
            "S -> a _ S c, b/c/$");
        AssertState(
            table,
            out var i2,
            "S -> d _ b, b/c/$");
        AssertState(
            table,
            out var i3,
            "START -> S _, $");
        AssertState(
            table,
            out var i4,
            "S -> d b _, b/c/$");
        AssertState(
            table,
            out var i5,
            "S -> a S _ b, b/c/$",
            "S -> a S _ c, b/c/$");
        AssertState(
            table,
            out var i6,
            "S -> a S b _, b/c/$");
        AssertState(
            table,
            out var i7,
            "S -> a S c _, b/c/$");

        // Assert action table
        AssertAction(table, i0, "a", Shift(i1));
        AssertAction(table, i0, "d", Shift(i2));
        AssertAction(table, i1, "a", Shift(i1));
        AssertAction(table, i1, "d", Shift(i2));
        AssertAction(table, i2, "b", Shift(i4));
        AssertAction(table, i3, "$", LrAction.Accept.Instance);
        AssertAction(table, i5, "b", Shift(i6));
        AssertAction(table, i5, "c", Shift(i7));
        foreach (var term in new[] { "$", "b", "c" })
        {
            AssertAction(table, i4, term, Reduce(grammar, "S -> d b"));
            AssertAction(table, i6, term, Reduce(grammar, "S -> a S b"));
            AssertAction(table, i7, term, Reduce(grammar, "S -> a S c"));
        }

        // Assert goto table
        Assert.Equal(i3, table.Goto[i0, new("S")]);
        Assert.Equal(i5, table.Goto[i1, new("S")]);
    }

    [Fact]
    public void FromSlrGrammar()
    {
        var grammar = TestUtils.ParseCfg(SlrGrammar);
        var table = LrParsingTable.Lalr(grammar);

        // Assert state count
        Assert.Equal(5, table.StateAllocator.States.Count);

        // Assert item sets
        AssertState(
            table,
            out var i0,
            "START -> _ S, $");
        AssertState(
            table,
            out var i1,
            "E -> 1 _ E, $",
            "E -> 1 _, $");
        AssertState(
            table,
            out var i2,
            "START -> S _, $");
        AssertState(
            table,
            out var i3,
            "S -> E _, $");
        AssertState(
            table,
            out var i4,
            "E -> 1 E _, $");

        // Assert action table
        AssertAction(table, i0, "1", Shift(i1));
        AssertAction(table, i1, "$", Reduce(grammar, "E -> 1"));
        AssertAction(table, i1, "1", Shift(i1));
        AssertAction(table, i2, "$", LrAction.Accept.Instance);
        AssertAction(table, i3, "$", Reduce(grammar, "S -> E"));
        AssertAction(table, i4, "$", Reduce(grammar, "E -> 1 E"));

        // Assert goto table
        Assert.Equal(i2, table.Goto[i0, new("S")]);
        Assert.Equal(i3, table.Goto[i0, new("E")]);
        Assert.Equal(i4, table.Goto[i1, new("E")]);
    }

    [Fact]
    public void FromLalrGrammar()
    {
        var grammar = TestUtils.ParseCfg(LalrGrammar);
        var table = LrParsingTable.Lalr(grammar);

        // Assert state count
        Assert.Equal(11, table.StateAllocator.States.Count);

        // Assert item sets
        AssertState(
            table,
            out var i0,
            "START -> _ S, $");
        AssertState(
            table,
            out var i1,
            "S -> a _ A c, $",
            "S -> a _ B d, $");
        AssertState(
            table,
            out var i2,
            "B -> z _, c");
        AssertState(
            table,
            out var i3,
            "START -> S _, $");
        AssertState(
            table,
            out var i4,
            "S -> B _ c, $");
        AssertState(
            table,
            out var i5,
            "S -> B c _, $");
        AssertState(
            table,
            out var i6,
            "A -> z _, c",
            "B -> z _, d");
        AssertState(
            table,
            out var i7,
            "S -> a A _ c, $");
        AssertState(
            table,
            out var i8,
            "S -> a B _ d, $");
        AssertState(
            table,
            out var i9,
            "S -> a B d _, $");
        AssertState(
            table,
            out var i10,
            "S -> a A c _, $");

        // Assert action table
        AssertAction(table, i0, "a", Shift(i1));
        AssertAction(table, i0, "z", Shift(i2));
        AssertAction(table, i1, "z", Shift(i6));
        AssertAction(table, i2, "c", Reduce(grammar, "B -> z"));
        AssertAction(table, i3, "$", LrAction.Accept.Instance);
        AssertAction(table, i4, "c", Shift(i5));
        AssertAction(table, i5, "$", Reduce(grammar, "S -> B c"));
        AssertAction(table, i6, "c", Reduce(grammar, "A -> z"));
        AssertAction(table, i6, "d", Reduce(grammar, "B -> z"));
        AssertAction(table, i7, "c", Shift(i10));
        AssertAction(table, i8, "d", Shift(i9));
        AssertAction(table, i9, "$", Reduce(grammar, "S -> a B d"));
        AssertAction(table, i10, "$", Reduce(grammar, "S -> a A c"));

        // Assert goto table
        Assert.Equal(i3, table.Goto[i0, new("S")]);
        Assert.Equal(i4, table.Goto[i0, new("B")]);
        Assert.Equal(i7, table.Goto[i1, new("A")]);
        Assert.Equal(i8, table.Goto[i1, new("B")]);
    }

    [Fact]
    public void FromClrGrammar()
    {
        var grammar = TestUtils.ParseCfg(ClrGrammar);
        var table = LrParsingTable.Lalr(grammar);

        // Assert state count
        Assert.Equal(13, table.StateAllocator.States.Count);

        // Assert item sets
        AssertState(
            table,
            out var i0,
            "START -> _ S, $");
        AssertState(
            table,
            out var i1,
            "S -> a _ E a, $",
            "S -> a _ F b, $");
        AssertState(
            table,
            out var i2,
            "S -> b _ E b, $",
            "S -> b _ F a, $");
        AssertState(
            table,
            out var i3,
            "START -> S _, $");
        AssertState(
            table,
            out var i4,
            "E -> e _, a/b",
            "F -> e _, a/b");
        AssertState(
            table,
            out var i5,
            "S -> b E _ b, $");
        AssertState(
            table,
            out var i6,
            "S -> b F _ a, $");
        AssertState(
            table,
            out var i7,
            "S -> b F a _, $");
        AssertState(
            table,
            out var i8,
            "S -> b E b _, $");
        AssertState(
            table,
            out var i9,
            "S -> a E _ a, $");
        AssertState(
            table,
            out var i10,
            "S -> a F _ b, $");
        AssertState(
            table,
            out var i11,
            "S -> a F b _, $");
        AssertState(
            table,
            out var i12,
            "S -> a E a _, $");

        // Assert action table
        AssertAction(table, i0, "a", Shift(i1));
        AssertAction(table, i0, "b", Shift(i2));
        AssertAction(table, i1, "e", Shift(i4));
        AssertAction(table, i2, "e", Shift(i4));
        AssertAction(table, i3, "$", LrAction.Accept.Instance);
        AssertAction(table, i4, "a", Reduce(grammar, "E -> e"), Reduce(grammar, "F -> e"));
        AssertAction(table, i4, "b", Reduce(grammar, "E -> e"), Reduce(grammar, "F -> e"));
        AssertAction(table, i5, "b", Shift(i8));
        AssertAction(table, i6, "a", Shift(i7));
        AssertAction(table, i7, "$", Reduce(grammar, "S -> b F a"));
        AssertAction(table, i8, "$", Reduce(grammar, "S -> b E b"));
        AssertAction(table, i9, "a", Shift(i12));
        AssertAction(table, i10, "b", Shift(i11));
        AssertAction(table, i11, "$", Reduce(grammar, "S -> a F b"));
        AssertAction(table, i12, "$", Reduce(grammar, "S -> a E a"));

        // Assert goto table
        Assert.Equal(i3, table.Goto[i0, new("S")]);
        Assert.Equal(i9, table.Goto[i1, new("E")]);
        Assert.Equal(i10, table.Goto[i1, new("F")]);
        Assert.Equal(i5, table.Goto[i2, new("E")]);
        Assert.Equal(i6, table.Goto[i2, new("F")]);
    }
}
