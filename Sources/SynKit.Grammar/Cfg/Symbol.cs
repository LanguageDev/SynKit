namespace SynKit.Grammar.Cfg;

/// <summary>
/// Represents a symbol that can appear in a context-free grammar.
/// </summary>
public abstract record Symbol
{
    private sealed class Marker
    {
        private readonly string text;

        public Marker(string text) => this.text = text;

        public override string ToString() => this.text;
    }

    /// <summary>
    /// Represents a terminal symbol.
    /// </summary>
    /// <param name="Value">The value identifying the terminal.</param>
    public sealed record Terminal(object Value) : Symbol
    {
        /// <summary>
        /// An end-of-input marker.
        /// </summary>
        public static Terminal EndOfInput { get; } = new(new Marker("$"));

        /// <summary>
        /// A terminal guaranteed to be not in any grammar.
        /// </summary>
        public static Terminal NotInGrammar { get; } = new(new Marker("#"));

        /// <inheritdoc/>
        public override string ToString() => this.Value.ToString() ?? "null";
    }

    /// <summary>
    /// Represents a nonterminal symbol.
    /// </summary>
    /// <param name="Value">The value identifying the nonterminal.</param>
    public sealed record Nonterminal(object Value) : Symbol
    {
        /// <inheritdoc/>
        public override string ToString() => this.Value.ToString() ?? "null";

        /// <summary>
        /// Creates a fresh nonterminal symbol from this one.
        /// </summary>
        /// <returns>A nonterminal, that is different from this.</returns>
        public Nonterminal Fresh() => new(new Marker($"{this.Value}'"));
    }

    /// <summary>
    /// Represents an epsilon symbol (empty word).
    /// </summary>
    public sealed record Epsilon : Symbol
    {
        /// <summary>
        /// The singleton instance to use.
        /// </summary>
        public static Epsilon Instance { get; } = new();

        private Epsilon()
        {
        }

        /// <inheritdoc/>
        public override string ToString() => "ε";
    }
}
