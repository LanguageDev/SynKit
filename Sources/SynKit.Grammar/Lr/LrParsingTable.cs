using SynKit.Grammar.Cfg;

namespace SynKit.Grammar.Lr;

/// <summary>
/// LR table construction functionality.
/// </summary>
public static class LrParsingTable
{
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

    private static LrItemSet<Lr0Item> Lr0Closure(
        ContextFreeGrammar grammar,
        IEnumerable<Lr0Item> set) => Closure(grammar, set, (item, prod) => new[] { new Lr0Item(prod, 0) });

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
        foreach (var item in set) stk.Push(item);
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
}
