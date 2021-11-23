using SynKit.Grammar.Cfg;
using System.Diagnostics;

namespace SynKit.Grammar.Lr;

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
    public static LrParsingTable<Lr0Item> Lr0(ContextFreeGrammar grammar)
    {
        if (grammar.StartSymbol is null) throw new InvalidOperationException("The grammar must have a start symbol!");

        var startProductions = grammar.GetProductions(grammar.StartSymbol);

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
            var itemsWithTerminals = itemSet.Items
                .Where(prod => prod.AfterCursor is Symbol.Terminal)
                .GroupBy(prod => prod.AfterCursor);
            foreach (var group in itemsWithTerminals)
            {
                var term = (Symbol.Terminal)group.Key!;
                var nextSet = Lr0Closure(grammar, group.Select(i => i.Next));
                if (stateAllocator.Allocate(nextSet, out var nextState)) stk.Push((nextSet, nextState));
                actionTable[state, term].Add(new LrAction.Shift(nextState));
            }

            // Nonterminal advance
            var itemsWithNonterminals = itemSet.Items
                .Where(prod => prod.AfterCursor is Symbol.Nonterminal)
                .GroupBy(prod => prod.AfterCursor);
            foreach (var group in itemsWithNonterminals)
            {
                var nonterm = (Symbol.Nonterminal)group.Key!;
                var nextSet = Lr0Closure(grammar, group.Select(i => i.Next));
                if (stateAllocator.Allocate(nextSet, out var nextState)) stk.Push((nextSet, nextState));
                gotoTable[state, nonterm] = nextState;
            }

            // Final items
            var finalItems = itemSet.Items.Where(prod => prod.IsFinal);
            foreach (var finalItem in finalItems)
            {
                if (finalItem.Production.Left.Equals(grammar.StartSymbol))
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

        return new(grammar, stateAllocator, actionTable, gotoTable);
    }

    /// <summary>
    /// Builds an SLR parsing table.
    /// </summary>
    /// <param name="grammar">The grammar to build the table for.</param>
    /// <returns>The SLR table for <paramref name="grammar"/>.</returns>
    public static LrParsingTable<Lr0Item> Slr(ContextFreeGrammar grammar)
    {
        if (grammar.StartSymbol is null) throw new InvalidOperationException("The grammar must have a start symbol!");

        var startProductions = grammar.GetProductions(grammar.StartSymbol);

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
            var itemsWithTerminals = itemSet.Items
                .Where(prod => prod.AfterCursor is Symbol.Terminal)
                .GroupBy(prod => prod.AfterCursor);
            foreach (var group in itemsWithTerminals)
            {
                var term = (Symbol.Terminal)group.Key!;
                var nextSet = Lr0Closure(grammar, group.Select(i => i.Next));
                if (stateAllocator.Allocate(nextSet, out var nextState)) stk.Push((nextSet, nextState));
                actionTable[state, term].Add(new LrAction.Shift(nextState));
            }

            // Nonterminal advance
            var itemsWithNonterminals = itemSet.Items
                .Where(prod => prod.AfterCursor is Symbol.Nonterminal)
                .GroupBy(prod => prod.AfterCursor);
            foreach (var group in itemsWithNonterminals)
            {
                var nonterm = (Symbol.Nonterminal)group.Key!;
                var nextSet = Lr0Closure(grammar, group.Select(i => i.Next));
                if (stateAllocator.Allocate(nextSet, out var nextState)) stk.Push((nextSet, nextState));
                gotoTable[state, nonterm] = nextState;
            }

            // Final items
            var finalItems = itemSet.Items.Where(prod => prod.IsFinal);
            foreach (var finalItem in finalItems)
            {
                if (finalItem.Production.Left.Equals(grammar.StartSymbol))
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

        return new(grammar, stateAllocator, actionTable, gotoTable);
    }

    /// <summary>
    /// Builds a LALR parsing table.
    /// </summary>
    /// <param name="grammar">The grammar to build the table for.</param>
    /// <returns>The LALR table for <paramref name="grammar"/>.</returns>
    public static LrParsingTable<LalrItem> Lalr(ContextFreeGrammar grammar)
    {
        LrItemSet<Lr0Item> ToKernelSet(LrItemSet<Lr0Item> itemSet) => new(itemSet.Items
            .Where(i => IsKernel(grammar, i))
            .ToHashSet());

        if (grammar.StartSymbol is null) throw new InvalidOperationException("The grammar must have a start symbol!");

        var startProductions = grammar.GetProductions(grammar.StartSymbol);

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
            var itemsWithTerminals = itemSet.Items
                .Where(prod => prod.AfterCursor is Symbol.Terminal)
                .GroupBy(prod => prod.AfterCursor);
            foreach (var group in itemsWithTerminals)
            {
                var term = (Symbol.Terminal)group.Key!;
                var nextSet = Lr0Closure(grammar, group.Select(i => i.Next));
                if (lr0StateAllocator.Allocate(ToKernelSet(nextSet), out var nextState)) stk.Push((nextSet, nextState));
                actionTable[state, term].Add(new LrAction.Shift(nextState));
            }

            // Nonterminal advance
            var itemsWithNonterminals = itemSet.Items
                .Where(prod => prod.AfterCursor is Symbol.Nonterminal)
                .GroupBy(prod => prod.AfterCursor);
            foreach (var group in itemsWithNonterminals)
            {
                var nonterm = (Symbol.Nonterminal)group.Key!;
                var nextSet = Lr0Closure(grammar, group.Select(i => i.Next));
                if (lr0StateAllocator.Allocate(ToKernelSet(nextSet), out var nextState)) stk.Push((nextSet, nextState));
                gotoTable[state, nonterm] = nextState;
            }
        }

        // Compute propagation information
        var lr0Table = new LrParsingTable<Lr0Item>(grammar, lr0StateAllocator, actionTable, gotoTable);
        var (generatesFrom, propagatesFrom) = GenerateLalrLookaheadInfo(lr0Table);

        // We create an LALR item mapping from the LR0 items
        // At the same time we write the spontaneous terminals into the lookaheads
        var lalrItemSets = new Dictionary<(LrState, Lr0Item), LalrItem>();
        foreach (var state in lr0StateAllocator.States)
        {
            foreach (var item in lr0StateAllocator[state].Items)
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
            var lalrItemSet = lr0StateAllocator[state].Items
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
            // Final items
            var finalItems = itemSet.Items.Where(prod => prod.IsFinal);
            foreach (var finalItem in finalItems)
            {
                if (finalItem.Production.Left.Equals(grammar.StartSymbol))
                {
                    actionTable[state, Symbol.Terminal.EndOfInput].Add(LrAction.Accept.Instance);
                }
                else
                {
                    var reduction = new LrAction.Reduce(finalItem.Production);
                    foreach (var lookahead in finalItem.Lookaheads) actionTable[state, lookahead].Add(reduction);
                }
            }
        }

        return new(grammar, stateAllocator, actionTable, gotoTable);
    }

    /// <summary>
    /// Builds a CLR (aka. LR(1)) parsing table.
    /// </summary>
    /// <param name="grammar">The grammar to build the table for.</param>
    /// <returns>The CLR table for <paramref name="grammar"/>.</returns>
    public static LrParsingTable<ClrItem> Clr(ContextFreeGrammar grammar)
    {
        if (grammar.StartSymbol is null) throw new InvalidOperationException("The grammar must have a start symbol!");

        var startProductions = grammar.GetProductions(grammar.StartSymbol);

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
            var itemsWithTerminals = itemSet.Items
                .Where(prod => prod.AfterCursor is Symbol.Terminal)
                .GroupBy(prod => prod.AfterCursor);
            foreach (var group in itemsWithTerminals)
            {
                var term = (Symbol.Terminal)group.Key!;
                var nextSet = ClrClosure(grammar, group.Select(i => i.Next));
                if (stateAllocator.Allocate(nextSet, out var nextState)) stk.Push((nextSet, nextState));
                actionTable[state, term].Add(new LrAction.Shift(nextState));
            }

            // Nonterminal advance
            var itemsWithNonterminals = itemSet.Items
                .Where(prod => prod.AfterCursor is Symbol.Nonterminal)
                .GroupBy(prod => prod.AfterCursor);
            foreach (var group in itemsWithNonterminals)
            {
                var nonterm = (Symbol.Nonterminal)group.Key!;
                var nextSet = ClrClosure(grammar, group.Select(i => i.Next));
                if (stateAllocator.Allocate(nextSet, out var nextState)) stk.Push((nextSet, nextState));
                gotoTable[state, nonterm] = nextState;
            }

            // Final items
            var finalItems = itemSet.Items.Where(prod => prod.IsFinal);
            foreach (var finalItem in finalItems)
            {
                if (finalItem.Production.Left.Equals(grammar.StartSymbol))
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

        return new(grammar, stateAllocator, actionTable, gotoTable);
    }

    private static LrItemSet<Lr0Item> Lr0Closure(
        ContextFreeGrammar grammar,
        IEnumerable<Lr0Item> set) => Closure(grammar, set, (item, prod) => new[] { new Lr0Item(prod, 0) });

    private static LrItemSet<LalrItem> LalrClosure(
        ContextFreeGrammar grammar,
        Lr0Item item) => new(ClrClosure(
            grammar,
            new[] { new ClrItem(item.Production, item.Cursor, Symbol.Terminal.NotInGrammar) }).Items
            .GroupBy(item => new Lr0Item(item.Production, item.Cursor))
            .Select(g => new LalrItem(g.Key.Production, g.Key.Cursor, g.Select(i => i.Lookahead).ToHashSet()))
            .ToHashSet());

    private static LrItemSet<ClrItem> ClrClosure(
        ContextFreeGrammar grammar,
        IEnumerable<ClrItem> set) => Closure(
            grammar,
            set,
            (item, prod) => GetClrClosureItems(grammar, item, prod));

    // Generic LR closure
    private static LrItemSet<TItem> Closure<TItem>(
        ContextFreeGrammar grammar,
        IEnumerable<TItem> set,
        Func<TItem, Production, IEnumerable<TItem>> getItems)
        where TItem : ILrItem
    {
        var result = set.ToHashSet();
        var stk = new Stack<TItem>();
        foreach (var item in result) stk.Push(item);
        while (stk.TryPop(out var item))
        {
            var afterCursor = item.AfterCursor;
            if (afterCursor is not Symbol.Nonterminal nonterm) continue;
            // It must be a nonterminal
            var prods = grammar.GetProductions(nonterm);
            foreach (var prod in prods)
            {
                var itemsToAdd = getItems(item, prod);
                foreach (var itemToAdd in itemsToAdd)
                {
                    if (result.Add(itemToAdd)) stk.Push(itemToAdd);
                }
            }
        }
        return new(result);
    }

    private static IEnumerable<ClrItem> GetClrClosureItems(
        ContextFreeGrammar grammar,
        ClrItem item,
        Production prod)
    {
        // Construct the sequence consisting of everything after the nonterminal plus the lookahead
        var after = item.Production.Right.Skip(item.Cursor + 1).Append(item.Lookahead);
        // Compute the first-set
        var firstSet = grammar.FirstSet(after);
        // Yield returns
        foreach (var term in firstSet.OfType<Symbol.Terminal>()) yield return new(prod, 0, term);
    }

    private static bool IsKernel<TItem>(ContextFreeGrammar grammar, TItem item)
        where TItem : ILrItem => item.Production.Left.Equals(grammar.StartSymbol)
                              || !item.IsInitial;

    private record struct LookaheadInfo(
        Dictionary<(LrState State, Lr0Item Item), HashSet<Symbol.Terminal>> GeneratesFrom,
        Dictionary<(LrState State, Lr0Item Item), List<(LrState State, Lr0Item Item)>> PropagatesFrom);

    private static LookaheadInfo GenerateLalrLookaheadInfo(LrParsingTable<Lr0Item> lr0Table)
    {
        var generatesFrom = new Dictionary<(LrState State, Lr0Item Item), HashSet<Symbol.Terminal>>();
        var propagatesFrom = new Dictionary<(LrState State, Lr0Item Item), List<(LrState State, Lr0Item Item)>>();

        // $ generates from the initial item
        var initialProductions = lr0Table.Grammar.GetProductions(lr0Table.Grammar.StartSymbol!);
        var initialItems = initialProductions.Select(p => new Lr0Item(p, 0));
        foreach (var initialItem in initialItems)
        {
            generatesFrom[(LrState.Initial, initialItem)] = new() { Symbol.Terminal.EndOfInput };
        }

        foreach (var fromState in lr0Table.StateAllocator.States)
        {
            var kernelItems = lr0Table.StateAllocator[fromState];
            foreach (var kernelItem in kernelItems.Items)
            {
                var kernelClosure = LalrClosure(lr0Table.Grammar, kernelItem);
                foreach (var closureItem in kernelClosure.Items)
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
