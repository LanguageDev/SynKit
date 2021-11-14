using SynKit.Grammar.Cfg;

namespace SynKit.Grammar.Lr.Internal;

internal sealed class LookaheadSensitivePath<TItem>
    where TItem : ILrItem
{
    private readonly LrParsingTable<TItem> table;

    public LookaheadSensitivePath(LrParsingTable<TItem> table)
    {
        this.table = table;
    }

    private void SearchPath()
    {
        // Simple BFS
        var queue = new Queue<LookaheadSensitiveState<TItem>>();
        // Initial is (s0, item0, {$})
        var item0 = table.StateAllocator[LrState.Initial].Items
            .First(i => i.Production.Left.Equals(this.table.Grammar.StartSymbol));
        queue.Enqueue(new(LrState.Initial, item0, new HashSet<Symbol.Terminal> { Symbol.Terminal.EndOfInput }));
        // While there's a next item in a queue, look up neighbours
        while (queue.TryDequeue(out var current))
        {
            // TODO: Check if we have arrived at the destination

            // Transition steps
            foreach (var (nextState, nextItem) in this.NextItem(current.State, current.Item))
            {
                queue.Enqueue(new(nextState, nextItem, current.Lookaheads));
            }

            // Production step
            if (current.Item.AfterCursor is Symbol.Nonterminal nt)
            {
                var prods = this.table.Grammar.GetProductions(nt);
                var follow = PreciseLookaheadSet(current.Item, current.Lookaheads);
                foreach (var nextProd in prods)
                {
                    queue.Enqueue(new(current.State, CreateItem(nextProd), follow));
                }
            }
        }
    }

    private IEnumerable<(LrState State, TItem Item)> NextItem(LrState state, TItem item)
    {
        var x = item.AfterCursor!;
        var nextItem = (TItem)item.Next;
        if (x is Symbol.Terminal t)
        {
            var actions = this.table.Action[state, t];
            foreach (var a in actions.OfType<LrAction.Shift>()) yield return (a.State, nextItem);
        }
        else
        {
            var nt = (Symbol.Nonterminal)x;
            var toState = this.table.Goto[state, nt]!.Value;
            yield return (toState, nextItem);
        }
    }

    private IReadOnlySet<Symbol.Terminal> PreciseLookaheadSet(TItem item, IReadOnlySet<Symbol.Terminal> context)
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
        var followNext = this.PreciseLookaheadSet((TItem)item.Next, context);
        first.UnionWith(followNext);
        return first;
    }

    private static TItem CreateItem(Production production) => throw new NotImplementedException();
}
