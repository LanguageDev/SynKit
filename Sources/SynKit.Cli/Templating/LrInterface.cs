using SynKit.Grammar.ContextFree;
using SynKit.Grammar.Lr;
using SynKit.Grammar.Lr.Items;
using SynKit.Grammar.Lr.Tables;

namespace SynKit.Cli.Templating;

/// <summary>
/// Interface for LR entities in templates.
/// </summary>
public static class LrInterface
{
    /// <summary>
    /// Retrieves the actions performed in the given LR table, when on a given state, encountering a given terminal.
    /// </summary>
    /// <param name="table">The LR table.</param>
    /// <param name="state">The current LR state.</param>
    /// <param name="term">The encountered terminal.</param>
    /// <returns>The actions performed, when the current state is <paramref name="state"/> and the encountered
    /// terminal is <paramref name="term"/>.</returns>
    public static ICollection<LrAction> LrActions(ILrParsingTable table, LrState state, Symbol.Terminal term) =>
        table.Action[state, term];

    /// <summary>
    /// Retrieves the destination state in the given LR table, when on a given state, after a reduction of a
    /// given nonterminal.
    /// </summary>
    /// <param name="table">The LR table.</param>
    /// <param name="state">The current LR state.</param>
    /// <param name="nonterm">The reduced nonterminal.</param>
    /// <returns>The state to go to, when the current state is <paramref name="state"/> and the reduced
    /// nonterminal is <paramref name="nonterm"/>.</returns>
    public static LrState? LrGoto(ILrParsingTable table, LrState state, Symbol.Nonterminal nonterm) =>
        table.Goto[state, nonterm];

    /// <summary>
    /// Checks, if a given LR action is a shift.
    /// </summary>
    /// <param name="action">The action to check.</param>
    /// <returns>True, if <paramref name="action"/> is a shift.</returns>
    public static bool IsShift(LrAction action) => action is LrAction.Shift;

    /// <summary>
    /// Checks, if a given LR action is a reduce.
    /// </summary>
    /// <param name="action">The action to check.</param>
    /// <returns>True, if <paramref name="action"/> is a reduce.</returns>
    public static bool IsReduce(LrAction action) => action is LrAction.Reduce;

    /// <summary>
    /// Checks, if a given LR action is an accept.
    /// </summary>
    /// <param name="action">The action to check.</param>
    /// <returns>True, if <paramref name="action"/> is an accept.</returns>
    public static bool IsAccept(LrAction action) => action is LrAction.Accept;

    /// <summary>
    /// Checks, if a given LR item is an LR0 item.
    /// </summary>
    /// <param name="item">The item to check.</param>
    /// <returns>True, if <paramref name="item"/> is LR0.</returns>
    public static bool IsLr0Item(ILrItem item) => item is Lr0Item;

    /// <summary>
    /// Checks, if a given LR item is a CLR item.
    /// </summary>
    /// <param name="item">The item to check.</param>
    /// <returns>True, if <paramref name="item"/> is CLR.</returns>
    public static bool IsClrItem(ILrItem item) => item is ClrItem;

    /// <summary>
    /// Checks, if a given LR item is an LALR item.
    /// </summary>
    /// <param name="item">The item to check.</param>
    /// <returns>True, if <paramref name="item"/> is LALR.</returns>
    public static bool IsLalrItem(ILrItem item) => item is LalrItem;
}
