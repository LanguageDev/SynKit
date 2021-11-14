using SynKit.Grammar.Cfg;

namespace SynKit.Grammar.Lr;

/// <summary>
/// Represents an action to perform for the LR parser.
/// </summary>
public abstract record LrAction
{
    /// <summary>
    /// Represents that the current token should be shifted.
    /// </summary>
    public sealed record Shift(LrState State) : LrAction
    {
        /// <inheritdoc/>
        public override string ToString() => $"shift({this.State.Id})";
    }

    /// <summary>
    /// Represents that the stack should be reduced using some rule.
    /// </summary>
    public sealed record Reduce(Production Production) : LrAction
    {
        /// <inheritdoc/>
        public override string ToString() => $"reduce({this.Production})";
    }

    /// <summary>
    /// An accept action, representing that the input is being accepted.
    /// </summary>
    public sealed record Accept : LrAction
    {
        /// <summary>
        /// The singleton instance to use.
        /// </summary>
        public static Accept Instance { get; } = new();

        private Accept()
        {
        }

        /// <inheritdoc/>
        public override string ToString() => "accept";
    }
}
