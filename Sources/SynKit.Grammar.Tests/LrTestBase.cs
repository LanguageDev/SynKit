using SynKit.Grammar.ContextFree;
using SynKit.Grammar.Lr;
using SynKit.Grammar.Lr.Items;
using SynKit.Grammar.Lr.Tables;
using System;
using System.Linq;
using Xunit;

namespace SynKit.Grammar.Tests;

public abstract class LrTestBase<TItem>
        where TItem : class, ILrItem
{
    /* Test grammars */

    public const string Lr0Grammar = @"
S -> a S b
S -> a S c
S -> d b
";

    public const string SlrGrammar = @"
S -> E
E -> 1 E
E -> 1
";

    public const string LalrGrammar = @"
S -> a A c
S -> a B d
S -> B c
A -> z
B -> z
";

    public const string ClrGrammar = @"
S -> a E a
S -> b E b
S -> a F b
S -> b F a
E -> e
F -> e
";

    /* Factory */

    protected static Production Production(CfGrammar cfg, string text) =>
        TestUtils.ParseProduction(cfg, text);

    protected static LrAction Shift(LrState state) =>
        new LrAction.Shift(state);

    protected static LrAction Reduce(CfGrammar cfg, string text) =>
        new LrAction.Reduce(TestUtils.ParseProduction(cfg, text));

    /* Assertions */

    protected static void AssertState(
        CfGrammar grammar,
        LrParsingTable<TItem> table,
        out LrState state,
        params string[] itemTexts)
    {
        var itemSet = itemTexts
            .Select(t => ParseItem(grammar, t))
            .OfType<TItem>()
            .ToHashSet();
        var found = table.StateItemSets
            .Where(si => si.ItemSet.SetEquals(itemSet))
            .GetEnumerator();
        Assert.True(found.MoveNext());
        state = found.Current.State;
    }

    protected static void AssertAction(
        LrParsingTable<TItem> table,
        LrState state,
        string term,
        params LrAction[] actions) =>
        AssertAction(table, state, term == "$" ? Symbol.Terminal.EndOfInput : new(term), actions);

    protected static void AssertAction(
        LrParsingTable<TItem> table,
        LrState state,
        Symbol.Terminal term,
        params LrAction[] actions)
    {
        var actualActions = table.Action[state, term].ToHashSet();
        Assert.True(actualActions.SetEquals(actions));
    }

    protected static ILrItem ParseItem(CfGrammar cfg, string text) =>
          typeof(TItem) == typeof(Lr0Item) ? TestUtils.ParseLr0Item(cfg, text)
        : typeof(TItem) == typeof(LalrItem) ? TestUtils.ParseLalrItem(cfg, text)
        : typeof(TItem) == typeof(ClrItem) ? TestUtils.ParseClrItem(cfg, text)
        : throw new NotSupportedException();
}
