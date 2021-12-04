using SynKit.Grammar.ContextFree;
using SynKit.Grammar.Internal;
using SynKit.Grammar.Lr.Internal;
using SynKit.Grammar.Lr.Items;
using System.Diagnostics;

namespace SynKit.Grammar.Lr.Tables;

/// <summary>
/// LR table construction functionality.
/// </summary>
public static class LrParsingTable
{
    // TODO: Plenty of repetition, maybe factor out common structure?

    /// <summary>
    /// Builds an LR(0) parsing table.
    /// </summary>
    /// <param name="grammar">The grammar to build the table for.</param>
    /// <returns>The LR(0) table for <paramref name="grammar"/>.</returns>
    public static LrParsingTable<Lr0Item> Lr0(CfGrammar grammar)
    {
        var startProductions = grammar.GetProductions(Symbol.Nonterminal.Start);

        var stateAllocator = new LrStateAllocator<Lr0Item>();
        var actionTable = new LrActionTable();
        var gotoTable = new LrGotoTable();

        // Construct the I0 set
        var i0 = Lr0Closure(grammar, startProductions.Select(p => new Lr0Item(p, 0)));
        var stk = new Stack<(LrItemSet<Lr0Item> ItemSet, LrState State)>();
        stateAllocator.Allocate(i0, out var state0);
        stk.Push((i0, state0));

        while (stk.TryPop(out var itemSetPair))
        {
            var itemSet = itemSetPair.ItemSet;
            var state = itemSetPair.State;

            // Terminal advance
            foreach (var group in itemSet.ShiftItems)
            {
                var nextSet = Lr0Closure(grammar, group.Select(i => i.Next));
                if (stateAllocator.Allocate(nextSet, out var nextState)) stk.Push((nextSet, nextState));
                actionTable[state, group.Key].Add(new LrAction.Shift(nextState));
            }

            // Nonterminal advance
            foreach (var group in itemSet.ProductionItems)
            {
                var nextSet = Lr0Closure(grammar, group.Select(i => i.Next));
                if (stateAllocator.Allocate(nextSet, out var nextState)) stk.Push((nextSet, nextState));
                gotoTable[state, group.Key] = nextState;
            }

            // Final items
            foreach (var finalItem in itemSet.ReductionItems)
            {
                if (finalItem.Production.Left.Equals(Symbol.Nonterminal.Start))
                {
                    actionTable[state, Symbol.Terminal.EndOfInput].Add(LrAction.Accept.Instance);
                }
                else
                {
                    var reduction = new LrAction.Reduce(finalItem.Production);
                    foreach (var term in grammar.Terminals) actionTable[state, term].Add(reduction);
                }
            }
        }

        return new(grammar.Terminals, grammar.Nonterminals, stateAllocator, actionTable, gotoTable);
    }

    /// <summary>
    /// Builds an SLR parsing table.
    /// </summary>
    /// <param name="grammar">The grammar to build the table for.</param>
    /// <returns>The SLR table for <paramref name="grammar"/>.</returns>
    public static LrParsingTable<Lr0Item> Slr(CfGrammar grammar)
    {
        var startProductions = grammar.GetProductions(Symbol.Nonterminal.Start);

        var stateAllocator = new LrStateAllocator<Lr0Item>();
        var actionTable = new LrActionTable();
        var gotoTable = new LrGotoTable();

        // Construct the I0 set
        var i0 = Lr0Closure(grammar, startProductions.Select(p => new Lr0Item(p, 0)));
        var stk = new Stack<(LrItemSet<Lr0Item> ItemSet, LrState State)>();
        stateAllocator.Allocate(i0, out var state0);
        stk.Push((i0, state0));

        while (stk.TryPop(out var itemSetPair))
        {
            var itemSet = itemSetPair.ItemSet;
            var state = itemSetPair.State;

            // Terminal advance
            foreach (var group in itemSet.ShiftItems)
            {
                var nextSet = Lr0Closure(grammar, group.Select(i => i.Next));
                if (stateAllocator.Allocate(nextSet, out var nextState)) stk.Push((nextSet, nextState));
                actionTable[state, group.Key].Add(new LrAction.Shift(nextState));
            }

            // Nonterminal advance
            foreach (var group in itemSet.ProductionItems)
            {
                var nextSet = Lr0Closure(grammar, group.Select(i => i.Next));
                if (stateAllocator.Allocate(nextSet, out var nextState)) stk.Push((nextSet, nextState));
                gotoTable[state, group.Key] = nextState;
            }

            // Final items
            foreach (var finalItem in itemSet.ReductionItems)
            {
                if (finalItem.Production.Left.Equals(Symbol.Nonterminal.Start))
                {
                    actionTable[state, Symbol.Terminal.EndOfInput].Add(LrAction.Accept.Instance);
                }
                else
                {
                    var reduction = new LrAction.Reduce(finalItem.Production);
                    var followSet = grammar.FollowSet(finalItem.Production.Left);
                    foreach (var follow in followSet) actionTable[state, follow].Add(reduction);
                }
            }
        }

        return new(grammar.Terminals, grammar.Nonterminals, stateAllocator, actionTable, gotoTable);
    }

    /// <summary>
    /// Builds a CLR (aka. LR(1)) parsing table.
    /// </summary>
    /// <param name="grammar">The grammar to build the table for.</param>
    /// <returns>The CLR table for <paramref name="grammar"/>.</returns>
    public static LrParsingTable<ClrItem> Clr(CfGrammar grammar)
    {
        var startProductions = grammar.GetProductions(Symbol.Nonterminal.Start);

        var stateAllocator = new LrStateAllocator<ClrItem>();
        var actionTable = new LrActionTable();
        var gotoTable = new LrGotoTable();

        // Construct the I0 set
        var i0 = ClrClosure(grammar, startProductions.Select(p => new ClrItem(p, 0, Symbol.Terminal.EndOfInput)));
        var stk = new Stack<(LrItemSet<ClrItem> ItemSet, LrState State)>();
        stateAllocator.Allocate(i0, out var state0);
        stk.Push((i0, state0));

        while (stk.TryPop(out var itemSetPair))
        {
            var itemSet = itemSetPair.ItemSet;
            var state = itemSetPair.State;

            // Terminal advance
            foreach (var group in itemSet.ShiftItems)
            {
                var nextSet = ClrClosure(grammar, group.Select(i => i.Next));
                if (stateAllocator.Allocate(nextSet, out var nextState)) stk.Push((nextSet, nextState));
                actionTable[state, group.Key].Add(new LrAction.Shift(nextState));
            }

            // Nonterminal advance
            foreach (var group in itemSet.ProductionItems)
            {
                var nextSet = ClrClosure(grammar, group.Select(i => i.Next));
                if (stateAllocator.Allocate(nextSet, out var nextState)) stk.Push((nextSet, nextState));
                gotoTable[state, group.Key] = nextState;
            }

            // Final items
            foreach (var finalItem in itemSet.ReductionItems)
            {
                if (finalItem.Production.Left.Equals(Symbol.Nonterminal.Start))
                {
                    actionTable[state, Symbol.Terminal.EndOfInput].Add(LrAction.Accept.Instance);
                }
                else
                {
                    var reduction = new LrAction.Reduce(finalItem.Production);
                    actionTable[state, finalItem.Lookahead].Add(reduction);
                }
            }
        }

        return new(grammar.Terminals, grammar.Nonterminals, stateAllocator, actionTable, gotoTable);
    }

    /// <summary>
    /// Builds a LALR parsing table.
    /// </summary>
    /// <param name="grammar">The grammar to build the table for.</param>
    /// <returns>The LALR table for <paramref name="grammar"/>.</returns>
    public static LrParsingTable<LalrItem> Lalr(CfGrammar grammar)
    {
        static LrItemSet<Lr0Item> ToKernelSet(LrItemSet<Lr0Item> itemSet) => new(itemSet.KernelItems);

        var startProductions = grammar.GetProductions(Symbol.Nonterminal.Start);

        var lr0StateAllocator = new LrStateAllocator<Lr0Item>();
        var actionTable = new LrActionTable();
        var gotoTable = new LrGotoTable();

        // Construct the I0 set
        var i0 = Lr0Closure(grammar, startProductions.Select(p => new Lr0Item(p, 0)));
        var stk = new Stack<(LrItemSet<Lr0Item> ItemSet, LrState State)>();
        lr0StateAllocator.Allocate(ToKernelSet(i0), out var state0);
        stk.Push((i0, state0));

        while (stk.TryPop(out var itemSetPair))
        {
            var itemSet = itemSetPair.ItemSet;
            var state = itemSetPair.State;

            // Terminal advance
            foreach (var group in itemSet.ShiftItems)
            {
                var nextSet = Lr0Closure(grammar, group.Select(i => i.Next));
                if (lr0StateAllocator.Allocate(ToKernelSet(nextSet), out var nextState)) stk.Push((nextSet, nextState));
                actionTable[state, group.Key].Add(new LrAction.Shift(nextState));
            }

            // Nonterminal advance
            foreach (var group in itemSet.ProductionItems)
            {
                var nextSet = Lr0Closure(grammar, group.Select(i => i.Next));
                if (lr0StateAllocator.Allocate(ToKernelSet(nextSet), out var nextState)) stk.Push((nextSet, nextState));
                gotoTable[state, group.Key] = nextState;
            }
        }

        // Compute propagation information
        var lr0Table = new LrParsingTable<Lr0Item>(grammar.Terminals, grammar.Nonterminals, lr0StateAllocator, actionTable, gotoTable);
        var (generatesFrom, propagatesFrom) = GenerateLalrLookaheadInfo(grammar, lr0Table);

        // We create an LALR item mapping from the LR0 items
        // At the same time we write the spontaneous terminals into the lookaheads
        var lalrItemSets = new Dictionary<(LrState, Lr0Item), LalrItem>();
        foreach (var state in lr0StateAllocator.States)
        {
            foreach (var item in lr0StateAllocator[state])
            {
                // If there are spontaneous terminal generations, we add that here
                if (!generatesFrom.TryGetValue((state, item), out var lookaheads)) lookaheads = new();
                lalrItemSets.Add((state, item), new(item.Production, item.Cursor, lookaheads));
            }
        }

        // Now we propagate as long as there is a change
        while (true)
        {
            var change = false;

            foreach (var ((fromState, fromItem), toList) in propagatesFrom)
            {
                var lookaheadsFrom = lalrItemSets[(fromState, fromItem)].Lookaheads;
                foreach (var (toState, toItem) in toList)
                {
                    var lookaheadsTo = (HashSet<Symbol.Terminal>)lalrItemSets[(toState, toItem)].Lookaheads;
                    foreach (var lookahead in lookaheadsFrom) change = lookaheadsTo.Add(lookahead) || change;
                }
            }

            if (!change) break;
        }

        // Reallocate states
        var stateAllocator = new LrStateAllocator<LalrItem>();
        foreach (var state in lr0StateAllocator.States)
        {
            var lalrItemSet = lr0StateAllocator[state]
                .Select(i => lalrItemSets[(state, i)])
                .ToHashSet();
            // NOTE: We depend on the fact that the order is the same
            stateAllocator.Allocate(new(lalrItemSet), out var gotState);
            Debug.Assert(state == gotState);
        }

        // Now we can assign the final items properly
        foreach (var state in stateAllocator.States)
        {
            var itemSet = stateAllocator[state];
            var clrItemSet = ClrClosure(
                grammar,
                itemSet.SelectMany(i => i.Lookaheads.Select(l => new ClrItem(i.Production, i.Cursor, l))));
            // Final items
            foreach (var finalItem in clrItemSet.ReductionItems)
            {
                if (finalItem.Production.Left.Equals(Symbol.Nonterminal.Start))
                {
                    actionTable[state, Symbol.Terminal.EndOfInput].Add(LrAction.Accept.Instance);
                }
                else
                {
                    var reduction = new LrAction.Reduce(finalItem.Production);
                    actionTable[state, finalItem.Lookahead].Add(reduction);
                }
            }
        }

        return new(grammar.Terminals, grammar.Nonterminals, stateAllocator, actionTable, gotoTable);
    }

    private static IEnumerable<Production> ItemProductions<TItem>(CfGrammar grammar, TItem item)
        where TItem : ILrItem => item.AfterCursor is Symbol.Nonterminal nonterm
        ? grammar.GetProductions(nonterm)
        : Enumerable.Empty<Production>();

    private static LrItemSet<Lr0Item> Lr0Closure(
        CfGrammar grammar,
        IEnumerable<Lr0Item> set) => new(GraphSearch.Dfs(
            set,
            // Simply get all the productions that have the current nonterminal on the left
            item => ItemProductions(grammar, item).Select(i => new Lr0Item(i, 0))));

    private static LrItemSet<ClrItem> ClrClosure(
        CfGrammar grammar,
        IEnumerable<ClrItem> set) => new(GraphSearch.Dfs(
            set,
            item => ItemProductions(grammar, item).SelectMany(prod =>
            {
                // Construct the sequence consisting of everything after the nonterminal plus the lookahead
                var after = item.Production.Right.Skip(item.Cursor + 1).Append(item.Lookahead);
                // Compute the first-set
                var firstSet = grammar.FirstSet(after);
                // Yield returns
                return firstSet.OfType<Symbol.Terminal>().Select(term => new ClrItem(prod, 0, term));
            })));

    private static LrItemSet<LalrItem> LalrClosure(
        CfGrammar grammar,
        Lr0Item item) => new(ClrClosure(
            grammar,
            new[] { new ClrItem(item.Production, item.Cursor, Symbol.Terminal.NotInGrammar) })
            .GroupBy(item => new Lr0Item(item.Production, item.Cursor))
            .Select(g => new LalrItem(g.Key.Production, g.Key.Cursor, g.Select(i => i.Lookahead).ToHashSet()))
            .ToHashSet());

    private readonly record struct LookaheadInfo(
        Dictionary<(LrState State, Lr0Item Item), HashSet<Symbol.Terminal>> GeneratesFrom,
        Dictionary<(LrState State, Lr0Item Item), List<(LrState State, Lr0Item Item)>> PropagatesFrom);

    private static LookaheadInfo GenerateLalrLookaheadInfo(CfGrammar grammar, LrParsingTable<Lr0Item> lr0Table)
    {
        var generatesFrom = new Dictionary<(LrState State, Lr0Item Item), HashSet<Symbol.Terminal>>();
        var propagatesFrom = new Dictionary<(LrState State, Lr0Item Item), List<(LrState State, Lr0Item Item)>>();

        // $ generates from the initial item
        var initialProductions = grammar.GetProductions(Symbol.Nonterminal.Start);
        var initialItems = initialProductions.Select(p => new Lr0Item(p, 0));
        foreach (var initialItem in initialItems)
        {
            generatesFrom[(LrState.Initial, initialItem)] = new() { Symbol.Terminal.EndOfInput };
        }

        foreach (var (fromState, kernelItems) in lr0Table.StateItemSets)
        {
            foreach (var kernelItem in kernelItems)
            {
                var kernelClosure = LalrClosure(grammar, kernelItem);
                foreach (var closureItem in kernelClosure)
                {
                    if (closureItem.IsFinal) continue;

                    var toState = closureItem.AfterCursor is Symbol.Terminal t
                        ? lr0Table.Action[fromState, t].OfType<LrAction.Shift>().First().State
                        : lr0Table.Goto[fromState, (Symbol.Nonterminal)closureItem.AfterCursor!]!.Value;
                    var toLalrItem = closureItem.Next;
                    var toItem = new Lr0Item(toLalrItem.Production, toLalrItem.Cursor);

                    foreach (var lookahead in closureItem.Lookaheads)
                    {
                        if (lookahead.Equals(Symbol.Terminal.NotInGrammar))
                        {
                            // Propagation
                            if (!propagatesFrom.TryGetValue((fromState, kernelItem), out var toList))
                            {
                                toList = new();
                                propagatesFrom.Add((fromState, kernelItem), toList);
                            }
                            toList.Add((toState, toItem));
                        }
                        else
                        {
                            // Spontaneous generation
                            if (!generatesFrom!.TryGetValue((toState, toItem), out var terminalSet))
                            {
                                terminalSet = new();
                                generatesFrom.Add((toState, toItem), terminalSet);
                            }
                            terminalSet.Add(lookahead);
                        }
                    }
                }
            }
        }

        return new(generatesFrom, propagatesFrom);
    }
}
