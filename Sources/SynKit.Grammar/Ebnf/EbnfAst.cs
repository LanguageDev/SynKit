namespace SynKit.Grammar.Ebnf;

/// <summary>
/// Base class for eBNF AST nodes.
/// </summary>
public abstract record EbnfAst
{
    /// <summary>
    /// Represents an eBNF rule reference.
    /// </summary>
    /// <param name="Name">The rule name.</param>
    public sealed record Rule(string Name) : EbnfAst;

    /// <summary>
    /// Represents a terminal value.
    /// </summary>
    /// <param name="Value">The object identifying the terminal.</param>
    public sealed record Term(object Value) : EbnfAst;

    /// <summary>
    /// Represents an eBNF node of two alternative constructs.
    /// </summary>
    /// <param name="Left">The first alternative.</param>
    /// <param name="Right">The second alternative.</param>
    public sealed record Alt(EbnfAst Left, EbnfAst Right) : EbnfAst;

    /// <summary>
    /// Represents a sequence - or concatenation - of two eBNF nodes.
    /// </summary>
    /// <param name="Left">The first node.</param>
    /// <param name="Right">The second node.</param>
    public sealed record Seq(EbnfAst Left, EbnfAst Right) : EbnfAst;

    /// <summary>
    /// Represents an optional version of another eBNF construct.
    /// </summary>
    /// <param name="Element">The construct to make optional.</param>
    public sealed record Opt(EbnfAst Element) : EbnfAst;

    /// <summary>
    /// Represents a 0-or-more repetition of another eBNF construct.
    /// </summary>
    /// <param name="Element">The construct to repeat.</param>
    public sealed record Rep0(EbnfAst Element) : EbnfAst;

    /// <summary>
    /// Represents a 1-or-more repetition of another eBNF construct.
    /// </summary>
    /// <param name="Element">The construct to repeat.</param>
    public sealed record Rep1(EbnfAst Element) : EbnfAst;

    /// <summary>
    /// Represents a repetition between an interval of times.
    /// </summary>
    public sealed record RepN : EbnfAst
    {
        /// <summary>
        /// The construct to repeat.
        /// </summary>
        public EbnfAst Element { get; }

        /// <summary>
        /// The minimum number of repetitions.
        /// </summary>
        public int Min { get; }

        /// <summary>
        /// The maximum number of repetitions. Null, if unbounded.
        /// </summary>
        public int? Max { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RepN"/> class.
        /// </summary>
        /// <param name="element">The construct to repeat.</param>
        /// <param name="min">The minimum amount of repetitions.</param>
        /// <param name="max">The maximum amount of repetitions. Null, if unbounded.</param>
        public RepN(EbnfAst element, int min, int? max)
        {
            if (min < 0) throw new ArgumentOutOfRangeException(nameof(min), "Min can't be less than 0.");
            if (max is not null && max.Value < min) throw new ArgumentOutOfRangeException(nameof(max), "Max can't be smaller than min.");

            this.Element = element;
            this.Min = min;
            this.Max = max;
        }
    }
}
