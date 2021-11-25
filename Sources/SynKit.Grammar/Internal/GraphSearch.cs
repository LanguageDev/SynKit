namespace SynKit.Grammar.Internal;

internal static class GraphSearch
{
    public static IReadOnlySet<T> Bfs<T>(T root, Func<T, IEnumerable<T>> getNeighbors) =>
        Bfs(new[] { root }, getNeighbors);

    public static IReadOnlySet<T> Bfs<T>(IEnumerable<T> roots, Func<T, IEnumerable<T>> getNeighbors) =>
        Bfs(roots, getNeighbors, EqualityComparer<T>.Default);

    public static IReadOnlySet<T> Bfs<T>(
        IEnumerable<T> roots,
        Func<T, IEnumerable<T>> getNeighbors,
        IEqualityComparer<T> comparer)
    {
        var labeled = roots.ToHashSet(comparer);
        var queue = new Queue<T>();
        foreach (var item in labeled) queue.Enqueue(item);
        while (queue.TryDequeue(out var v))
        {
            foreach (var n in getNeighbors(v))
            {
                if (labeled.Add(n)) queue.Enqueue(n);
            }
        }
        return labeled;
    }

    public static IReadOnlySet<T> Dfs<T>(T root, Func<T, IEnumerable<T>> getNeighbors) =>
        Dfs(new[] { root }, getNeighbors);

    public static IReadOnlySet<T> Dfs<T>(IEnumerable<T> roots, Func<T, IEnumerable<T>> getNeighbors) =>
        Dfs(roots, getNeighbors, EqualityComparer<T>.Default);

    public static IReadOnlySet<T> Dfs<T>(
        IEnumerable<T> roots,
        Func<T, IEnumerable<T>> getNeighbors,
        IEqualityComparer<T> comparer)
    {
        var labeled = roots.ToHashSet(comparer);
        var stack = new Stack<T>();
        foreach (var item in labeled) stack.Push(item);
        while (stack.TryPop(out var v))
        {
            foreach (var n in getNeighbors(v))
            {
                if (labeled.Add(n)) stack.Push(n);
            }
        }
        return labeled;
    }
}
