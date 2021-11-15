using SynKit.Grammar.Cfg;

namespace SynKit.Grammar.Lr.Internal;

internal sealed record LookaheadState(LrState State, Lr0Item Item, IReadOnlySet<Symbol.Terminal> Lookaheads);
