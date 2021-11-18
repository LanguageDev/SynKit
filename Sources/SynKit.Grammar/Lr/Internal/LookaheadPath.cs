using SynKit.Grammar.Cfg;
using System.Diagnostics;

namespace SynKit.Grammar.Lr.Internal;

internal sealed class LookaheadPath<TItem>
    where TItem : ILrItem
{
    private record SearchNode(LookaheadItem Item, SearchNode? Parent);

    private readonly LrParsingTable<TItem> table;
    private Dictionary<StateItem, Dictionary<Symbol, HashSet<StateItem>>>? reverseTransitions;

    public LookaheadPath(LrParsingTable<TItem> table)
    {
        this.table = table;
    }

    public IReadOnlyList<LookaheadItem> Search(LrState searchState, TItem searchItem, Symbol.Terminal searchTerm)
    {
        bool IsSearched(LookaheadItem state) =>
               state.State == searchState
            && Lr0Equals(state.Item, searchItem)
            && state.Lookaheads.Contains(searchTerm);

        if (!searchItem.IsFinal) throw new ArgumentException("The searched item must be a reduce one.", nameof(searchItem));

        // Calculate eligible state set
        var eligible = this.CalculateEligibleStates(new(searchState, ToLr0Item(searchItem)));

        // Simple BFS
        var queue = new Queue<SearchNode>();
        // Initial is (s0, item0, {$})
        var item0 = table.StateAllocator[LrState.Initial].Items
            .First(i => i.Production.Left.Equals(this.table.Grammar.StartSymbol));
        var lr0Item0 = new Lr0Item(item0.Production, item0.Cursor);
        var initial = new LookaheadItem(
            LrState.Initial, lr0Item0, new HashSet<Symbol.Terminal> { Symbol.Terminal.EndOfInput });
        queue.Enqueue(new(initial, null));
        // While there's a next item in a queue, look up neighbours
        while (queue.TryDequeue(out var currentNode))
        {
            var current = currentNode.Item;

            // NOTE: It would be more elegant to check here, but it's more space efficient to check new
            // states when adding them to the queue

            // Transition steps
            foreach (var (nextState, nextItem) in this.NextItem(current.State, current.Item))
            {
                var next = new LookaheadItem(nextState, nextItem, current.Lookaheads);
                if (!eligible.Contains(new(next.State, next.Item))) continue;
                if (IsSearched(next)) return YieldPath(new(next, currentNode));
                queue.Enqueue(new(next, currentNode));
            }

            // Production step
            if (current.Item.AfterCursor is Symbol.Nonterminal nt)
            {
                var prods = this.table.Grammar.GetProductions(nt);
                var follow = PreciseLookaheadSet(current.Item, current.Lookaheads);
                foreach (var nextProd in prods)
                {
                    var next = new LookaheadItem(current.State, CreateItem(nextProd), follow);
                    if (!eligible.Contains(new(next.State, next.Item))) continue;
                    if (IsSearched(next)) return YieldPath(new(next, currentNode));
                    queue.Enqueue(new(next, currentNode));
                }
            }
        }
        return Array.Empty<LookaheadItem>();
    }

    public IReadOnlyList<StateItem> DiscoverShiftPath(IReadOnlyList<StateItem> path, TItem shiftItem)
    {
        if (path.Count == 0) throw new ArgumentException("The path must not be empty.", nameof(path));

        var startItem = this.table.StateAllocator[LrState.Initial].Items
            .First(i => i.Production.Left.Equals(this.table.Grammar.StartSymbol));

        var si = new StateItem(path[^1].State, ToLr0Item(shiftItem));
        var result = new List<StateItem> { si };
        var itr = path.Count;
        // refsi is the last StateItem in this state of the shortest path.
        var refsi = path[--itr];
        while (refsi is not null)
        {
            // Construct a list of items in the same state as refsi.
            // prevrefsi is the last StateItem in the previous state.
            var refsis = new List<StateItem> { refsi };
            var prevrefsi = itr > 0 ? path[--itr] : null;
            if (prevrefsi is not null)
            {
                for (int curPos = refsi.Item.Cursor, prevPos = prevrefsi.Item.Cursor;
                     prevrefsi is not null && prevPos + 1 != curPos;)
                {
                    refsis.Insert(0, prevrefsi);
                    curPos = prevPos;
                    if (itr > 0)
                    {
                        prevrefsi = path[--itr];
                        prevPos = prevrefsi.Item.Cursor;
                    }
                    else prevrefsi = null;
                }
            }
            if (si.Equals(refsi) || Lr0Equals(si.Item, startItem))
            {
                // Reached common item; prepend to the beginning.
                refsis.RemoveAt(refsis.Count - 1);
                result.InsertRange(0, refsis);
                if (prevrefsi is not null) result.Insert(0, prevrefsi);
                while (itr > 0) result.Insert(0, path[--itr]);
                return result;
            }

            if (si.Item.IsInitial)
            {
                // For a production item, find a sequence of items within the
                // same state that leads to this production.
                var init = new List<StateItem> { si };
                var queue = new Queue<List<StateItem>>();
                queue.Enqueue(init);
                while (queue.TryDequeue(out var sis))
                {
                    StateItem sisrc = sis[0];
                    if (Lr0Equals(sisrc.Item, startItem))
                    {
                        sis.RemoveAt(sis.Count - 1);
                        result.InsertRange(0, sis);
                        si = sisrc;
                        break;
                    }
                    if (!sisrc.Item.IsInitial)
                    {
                        // Determine if reverse transition is possible.
                        int srcpos = sisrc.Item.Cursor;
                        var prod = sisrc.Item.Production;
                        var sym = prod.Right[srcpos - 1];
                        foreach (var prevsi in GetReverseTransitions(sisrc, sym))
                        {
                            // Only look for state compatible with the shortest path.
                            if (prevsi.State != prevrefsi!.State) continue;
                            sis.RemoveAt(sis.Count - 1);
                            result.InsertRange(0, sis);
                            result.Insert(0, prevsi);
                            si = prevsi;
                            refsi = prevrefsi;
                            queue.Clear();
                            break;
                        }
                    }
                    else
                    {
                        // Take a reverse production step if possible.
                        var prod = sisrc.Item.Production;
                        var lhs = prod.Left;
                        foreach (var prev in GetReverseProduction(sisrc.State, lhs))
                        {
                            var prevsi = LookupStateItem(sisrc.State, prev);
                            if (prevsi is not null && sis.Contains(prevsi)) continue;
                            var prevsis = new List<StateItem>(sis);
                            prevsis.Insert(0, prevsi!);
                            queue.Enqueue(prevsis);
                        }
                    }
                }
            }
            else
            {
                // If not a production item, make a reverse transition.
                var pos = si.Item.Cursor;
                var prod = si.Item.Production;
                var sym = prod.Right[pos - 1];
                foreach (var prevsi in GetReverseTransitions(si, sym))
                {
                    // Only look for state compatible with the shortest path.
                    if (prevsi.State != prevrefsi!.State) continue;
                    result.Insert(0, prevsi);
                    si = prevsi;
                    refsi = prevrefsi;
                    break;
                }
            }
        }
        throw new InvalidOperationException("Cannot find derivation to conflict state.");
    }

    public (IReadOnlyList<Symbol> Symbols, int Cursor) CompleteAllProductions(
        IReadOnlyList<StateItem> path,
        Symbol.Terminal lookahead)
    {
        if (path.Count == 0) throw new ArgumentException("The path must not be empty.", nameof(path));

        var result = path[0].Item.Production.Right.ToList();
        var offset = 0;

        for (var i = 1; i < path.Count; ++i)
        {
            var prevState = path[i - 1];
            var currState = path[i];

            var isTransition = prevState.Item.Cursor + 1 == currState.Item.Cursor;
            if (isTransition)
            {
                // For a transition, we just consume the symbol
                ++offset;
            }
            else
            {
                // We substitute the production rule
                result.RemoveAt(offset);
                result.InsertRange(offset, currState.Item.Production.Right);
            }
        }

        if (result[offset] is Symbol.Nonterminal)
        {
            // We need to substitute to get the proper lookahead terminal
            var usedProductions = new HashSet<Production>();
            while (result[offset] is Symbol.Nonterminal nt)
            {
                var productions = this.table.Grammar.GetProductions(nt);
                foreach (var prod in productions)
                {
                    // Epsilon-productions don't count
                    if (prod.Right.Count == 0) continue;
                    // We can consider the production, if the first-set contains the terminal
                    if (!this.table.Grammar.FirstSet(prod.Right).Contains(lookahead)) continue;
                    // Check, if already used
                    if (!usedProductions.Add(prod)) continue;
                    // Do the substitution
                    result.RemoveAt(offset);
                    result.InsertRange(offset, prod.Right);
                    break;
                }
            }
        }
        Debug.Assert(result[offset].Equals(lookahead), "The terminal after the cursor must be the lookahead terminal.");

        return (result, offset);
    }

    private IReadOnlySet<StateItem> CalculateEligibleStates(StateItem target)
    {
        this.reverseTransitions ??= this.CalculateReverseTransitions();
        var result = new HashSet<StateItem>();
        var queue = new Queue<StateItem>();
        queue.Enqueue(target);
        while (queue.TryDequeue(out var si))
        {
            if (!result.Add(si)) continue;
            // Consider reverse transitions and reverse productions.
            if (this.reverseTransitions.TryGetValue(si, out var onMap))
            {
                foreach (var prev in onMap.Values.SelectMany(x => x)) queue.Enqueue(prev);
            }
            if (si.Item.IsInitial)
            {
                var prod = si.Item.Production;
                var lhs = prod.Left;
                foreach (var prev in this.GetReverseProduction(si.State, lhs))
                {
                    queue.Enqueue(new(si.State, ToLr0Item(prev)));
                }
            }
        }
        return result;
    }

    private IEnumerable<(LrState State, Lr0Item Item)> NextItem(LrState state, Lr0Item item)
    {
        var x = item.AfterCursor!;
        var nextItem = item.Next;
        if (x is Symbol.Terminal t)
        {
            var actions = this.table.Action[state, t];
            foreach (var a in actions.OfType<LrAction.Shift>()) yield return (a.State, nextItem);
        }
        else
        {
            var nt = (Symbol.Nonterminal)x;
            var toState = this.table.Goto[state, nt];
            if (toState is not null) yield return (toState.Value, nextItem);
        }
    }

    private IReadOnlySet<Symbol.Terminal> PreciseLookaheadSet(Lr0Item item, IReadOnlySet<Symbol.Terminal> context)
    {
        // FOLLOW_L(A -> X1 ... X(n-1) _ Xn) = L
        if (item.Cursor + 1 == item.Production.Right.Count) return context;
        var x = item.Production.Right[item.Cursor + 1];
        // FOLLOW_L(A -> X1 ... Xk _ X(k+1) X(k+2) ... Xn) = {X(k+2)} if X(k+2) is a terminal
        if (x is Symbol.Terminal t) return new HashSet<Symbol.Terminal>() { t };
        var nx = (Symbol.Nonterminal)x;
        var first = this.table.Grammar.FirstSet(nx).OfType<Symbol.Terminal>().ToHashSet();
        // FOLLOW_L(A -> X1 ... Xk _ X(k+1) X(k+2) ... Xn) = FIRST(X(k+2)) if X(k+2) is a non-nullable nonterminal
        if (!this.table.Grammar.IsNullable(nx)) return first;
        // FOLLOW_L(A -> X1 ... Xk _ X(k+1) X(k+2) ... Xn) =
        //     FIRST(X(k+2)) U FOLLOW_L(A -> X1 ... Xk X(k+1) _ X(k+2) ... Xn) if X(k+2) is a nullable nonterminal
        var followNext = this.PreciseLookaheadSet(item.Next, context);
        first.UnionWith(followNext);
        return first;
    }

    private IEnumerable<TItem> GetReverseProduction(LrState state, Symbol.Nonterminal nt) =>
        this.table.StateAllocator[state].Items
            .Where(item => nt.Equals(item.AfterCursor))
            // NOTE: This sort is not required but can make the example shorter
            .OrderBy(item => item.Production.Right.Count);

    private StateItem? LookupStateItem(LrState state, TItem item)
    {
        var lr0Item = ToLr0Item(item);
        var items = Closure(this.table.StateAllocator[state].Items);
        return items.Contains(lr0Item) ? new(state, lr0Item) : null;
    }

    private IReadOnlySet<StateItem> GetReverseTransitions(StateItem item, Symbol symbol)
    {
        this.reverseTransitions ??= this.CalculateReverseTransitions();
        return this.reverseTransitions[item][symbol];
    }

    private Dictionary<StateItem, Dictionary<Symbol, HashSet<StateItem>>> CalculateReverseTransitions()
    {
        var result = new Dictionary<StateItem, Dictionary<Symbol, HashSet<StateItem>>>();

        void AddTransition(LrState srcState, LrState dstState, Symbol symbol)
        {
            foreach (var srcItem in this.Closure(this.table.StateAllocator[srcState].Items))
            {
                foreach (var dstItem in this.Closure(this.table.StateAllocator[dstState].Items))
                {
                    if (srcItem.Cursor + 1 != dstItem.Cursor) continue;
                    if (!srcItem.Production.Equals(dstItem.Production)) continue;
                    var dstStateItem = new StateItem(dstState, dstItem);
                    if (!result.TryGetValue(dstStateItem, out var symbolMap))
                    {
                        symbolMap = new();
                        result.Add(dstStateItem, symbolMap);
                    }
                    if (!symbolMap.TryGetValue(symbol, out var set))
                    {
                        set = new();
                        symbolMap.Add(symbol, set);
                    }
                    set.Add(new(srcState, srcItem));
                }
            }
        }

        foreach (var state in this.table.States)
        {
            // Check temrinal transitions
            foreach (var term in this.table.Grammar.Terminals)
            {
                var shifts = this.table.Action[state, term].OfType<LrAction.Shift>();
                foreach (var shift in shifts) AddTransition(state, shift.State, term);
            }
            // Check nonterminal transitions
            foreach (var nonterm in this.table.Grammar.Nonterminals)
            {
                var to = this.table.Goto[state, nonterm];
                if (to is not null) AddTransition(state, to.Value, nonterm);
            }
        }
        return result;
    }

    private HashSet<Lr0Item> Closure(IEnumerable<TItem> set)
    {
        var result = set.Select(ToLr0Item).ToHashSet();
        var stk = new Stack<Lr0Item>();
        foreach (var item in result) stk.Push(item);
        while (stk.TryPop(out var item))
        {
            var afterCursor = item.AfterCursor;
            if (afterCursor is not Symbol.Nonterminal nonterm) continue;
            // It must be a nonterminal
            var prods = this.table.Grammar.GetProductions(nonterm);
            foreach (var prod in prods)
            {
                var itemToAdd = new Lr0Item(prod, 0);
                if (result.Add(itemToAdd)) stk.Push(itemToAdd);
            }
        }
        return result;
    }

    private static IReadOnlyList<LookaheadItem> YieldPath(SearchNode node)
    {
        static IEnumerable<LookaheadItem> YieldHelper(SearchNode node)
        {
            for (SearchNode? it = node; it is not null; it = it.Parent) yield return it.Item;
        }

        var result = YieldHelper(node).ToList();
        result.Reverse();
        return result;
    }

    private static Lr0Item CreateItem(Production production) => new(production, 0);

    private static Lr0Item ToLr0Item(TItem item) => new(item.Production, item.Cursor);

    private static bool Lr0Equals(Lr0Item i1, TItem i2) =>
           i1.Production.Equals(i2.Production)
        && i1.Cursor == i2.Cursor;

    private static StateItem ToStateItem(LookaheadItem s) => new(s.State, s.Item);
}
