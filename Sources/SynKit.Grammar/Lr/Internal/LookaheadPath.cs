using SynKit.Grammar.Cfg;

namespace SynKit.Grammar.Lr.Internal;

internal sealed class LookaheadPath<TItem>
    where TItem : ILrItem
{
    private record SearchNode(LookaheadState Item, SearchNode? Parent);

    private readonly LrParsingTable<TItem> table;

    public LookaheadPath(LrParsingTable<TItem> table)
    {
        this.table = table;
    }

    public IReadOnlyList<LookaheadState> Search(LrState searchState, TItem searchItem, Symbol.Terminal searchTerm)
    {
        bool IsSearched(LookaheadState state) =>
               state.State == searchState
            && Lr0Equals(state.Item, searchItem)
            && state.Lookaheads.Contains(searchTerm);

        if (!searchItem.IsFinal) throw new ArgumentException("The searched item must be a reduce one.", nameof(searchItem));

        // Simple BFS
        var queue = new Queue<SearchNode>();
        // Initial is (s0, item0, {$})
        var item0 = table.StateAllocator[LrState.Initial].Items
            .First(i => i.Production.Left.Equals(this.table.Grammar.StartSymbol));
        var lr0Item0 = new Lr0Item(item0.Production, item0.Cursor);
        var initial = new LookaheadState(
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
                var next = new LookaheadState(nextState, nextItem, current.Lookaheads);
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
                    var next = new LookaheadState(current.State, CreateItem(nextProd), follow);
                    if (IsSearched(next)) return YieldPath(new(next, currentNode));
                    queue.Enqueue(new(next, currentNode));
                }
            }
        }
        return Array.Empty<LookaheadState>();
    }

    public (IReadOnlyList<Symbol> Symbols, int Cursor) CompleteAllProductions(IReadOnlyList<LookaheadState> path)
    {
        if (path.Count == 0) return (Array.Empty<Symbol>(), 0);

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

        return (result, offset);
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

    private static IReadOnlyList<LookaheadState> YieldPath(SearchNode node)
    {
        static IEnumerable<LookaheadState> YieldHelper(SearchNode node)
        {
            for (SearchNode? it = node; it is not null; it = it.Parent) yield return it.Item;
        }

        var result = YieldHelper(node).ToList();
        result.Reverse();
        return result;
    }

    private static Lr0Item CreateItem(Production production) => new(production, 0);

    private static bool Lr0Equals(Lr0Item i1, TItem i2) =>
           i1.Production.Equals(i2.Production)
        && i1.Cursor == i2.Cursor;
}
