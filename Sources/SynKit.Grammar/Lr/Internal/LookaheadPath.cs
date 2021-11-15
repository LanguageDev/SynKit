using SynKit.Grammar.Cfg;
using System.Diagnostics;

namespace SynKit.Grammar.Lr.Internal;

internal sealed class LookaheadPath<TItem>
    where TItem : ILrItem
{
    private record SearchNode(LookaheadState Item, SearchNode? Parent);

    private readonly LrParsingTable<TItem> table;
    private readonly LookaheadState conflicting;

    public LookaheadPath(
        LrParsingTable<TItem> table,
        LookaheadState conflicting)
    {
        Debug.Assert(conflicting.Item.IsFinal, "The item must be a reduce item.");
        Debug.Assert(conflicting.Lookaheads.Count == 1, "There must be one conflicting symbol.");

        this.table = table;
        this.conflicting = conflicting;
    }

    public void SearchPath()
    {
        static string DebugDumpPath(SearchNode s) =>
              (s.Parent is null ? string.Empty : DebugDumpPath(s.Parent))
            + $"\n[{s.Item.State}, {s.Item.Item}, {{{string.Join(", ", s.Item.Lookaheads)}}}";

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
                if (this.IsSearched(next))
                {
                    // TODO: Found path
                    Console.WriteLine(DebugDumpPath(new(next, currentNode)));
                }
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
                    if (this.IsSearched(next))
                    {
                        // TODO: Found path
                        Console.WriteLine(DebugDumpPath(new(next, currentNode)));
                    }
                    queue.Enqueue(new(next, currentNode));
                }
            }
        }
    }

    private bool IsSearched(LookaheadState state) =>
           state.State == this.conflicting.State
        && state.Item.Equals(this.conflicting.Item)
        && state.Lookaheads.Contains(this.conflicting.Lookaheads.First());

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

    private static Lr0Item CreateItem(Production production) => new(production, 0);
}
