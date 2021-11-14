using SynKit.Grammar.Cfg;

namespace SynKit.Grammar.Lr.Internal;

internal sealed record LookaheadSensitiveState<TItem>(LrState State, TItem Item, IReadOnlySet<Symbol.Terminal> Lookaheads)
    where TItem : ILrItem;
