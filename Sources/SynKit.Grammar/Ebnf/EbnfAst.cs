namespace SynKit.Grammar.Ebnf;

/// <summary>
/// Base class for eBNF AST nodes.
/// </summary>
public abstract record EbnfAst
{
    /// <summary>
    /// Creates a node that represents an optional element.
    /// </summary>
    /// <param name="element">The element to make optional.</param>
    /// <returns>A node that represents matching an optional <paramref name="element"/>.</returns>
    public static EbnfAst Opt(EbnfAst element) => new Alt(element, Epsilon.Instance);

    /// <summary>
    /// Creates a repetition node that represents 0 or more repetitions.
    /// </summary>
    /// <param name="element">The element to repeat.</param>
    /// <returns>A repetition node that represents matching <paramref name="element"/> 0 or more times.</returns>
    public static EbnfAst ZeroOrMore(EbnfAst element) => new Rep(element, 0, null);

    /// <summary>
    /// Creates a repetition node that represents 1 or more repetitions.
    /// </summary>
    /// <param name="element">The element to repeat.</param>
    /// <returns>A repetition node that represents matching <paramref name="element"/> 1 or more times.</returns>
    public static EbnfAst OneOrMore(EbnfAst element) => new Rep(element, 1, null);

    /// <summary>
    /// Creates a repetition node that represents at least a given amount of repetitions.
    /// </summary>
    /// <param name="element">The element to repeat.</param>
    /// <param name="n">The minimum number of repetitions.</param>
    /// <returns>A repetition node that represents matching <paramref name="element"/> <paramref name="n"/>
    /// or more times.</returns>
    public static EbnfAst AtLeast(EbnfAst element, int n) => new Rep(element, n, null);

    /// <summary>
    /// Creates a repetition node that represents at most a given amount of repetitions.
    /// </summary>
    /// <param name="element">The element to repeat.</param>
    /// <param name="n">The maximum number of repetitions.</param>
    /// <returns>A repetition node that represents matching <paramref name="element"/> <paramref name="n"/>
    /// or less times.</returns>
    public static EbnfAst AtMost(EbnfAst element, int n) => new Rep(element, 0, n);

    /// <summary>
    /// Creates a repetition node that represents repetition in a given range.
    /// </summary>
    /// <param name="element">The element to repeat.</param>
    /// <param name="min">The minimum number of repetitions.</param>
    /// <param name="max">The maximum number of repetitions.</param>
    /// <returns>A repetition node that represents matching <paramref name="element"/> between <paramref name="min"/>
    /// (inclusive) and <paramref name="max"/> (inclusive) times.</returns>
    public static Rep Between(EbnfAst element, int min, int max) => new(element, min, max);

    /// <summary>
    /// Creates a repetition node that represents repetition an exact amount of times.
    /// </summary>
    /// <param name="element">The element to repeat.</param>
    /// <param name="n">The number of repetitions.</param>
    /// <returns>A repetition node that represents matching <paramref name="element"/> exactly <paramref name="n"/>
    /// times.</returns>
    public static Rep Exactly(EbnfAst element, int n) => new(element, n, n);

    /// <summary>
    /// Normalizes this node so it contains alternatives on top, sequences below that, and only has 0-or-more
    /// unbounded repetitions.
    /// </summary>
    /// <returns>The normalized, equivalent eBNF tree.</returns>
    public abstract EbnfAst Normalize();

    /// <summary>
    /// Represents a node that always matches an empty word.
    /// </summary>
    public sealed record Epsilon : EbnfAst
    {
        /// <summary>
        /// The instance to use.
        /// </summary>
        public static Epsilon Instance { get; } = new();

        private Epsilon()
        {
        }

        /// <inheritdoc/>
        public override EbnfAst Normalize() => this;
    }

    /// <summary>
    /// Represents an eBNF reference by a name to a rule or terminal.
    /// </summary>
    /// <param name="Name">The reference name.</param>
    public sealed record Reference(string Name) : EbnfAst
    {
        /// <inheritdoc/>
        public override EbnfAst Normalize() => this;
    }

    /// <summary>
    /// Represents an eBNF node of two alternative constructs.
    /// </summary>
    /// <param name="Left">The first alternative.</param>
    /// <param name="Right">The second alternative.</param>
    public sealed record Alt(EbnfAst Left, EbnfAst Right) : EbnfAst
    {
        /// <inheritdoc/>
        public override EbnfAst Normalize() => new Alt(this.Left.Normalize(), this.Right.Normalize());
    }

    /// <summary>
    /// Represents a sequence - or concatenation - of two eBNF nodes.
    /// </summary>
    /// <param name="Left">The first node.</param>
    /// <param name="Right">The second node.</param>
    public sealed record Seq(EbnfAst Left, EbnfAst Right) : EbnfAst
    {
        /// <inheritdoc/>
        public override EbnfAst Normalize()
        {
            var leftNorm = this.Left.Normalize();
            var rightNorm = this.Right.Normalize();
            if (leftNorm is Epsilon) return rightNorm;
            if (rightNorm is Epsilon) return leftNorm;
            if (leftNorm is Alt leftAlt)
            {
                // (X | Y) Z => X Z | Y Z
                return new Alt(
                    new Seq(leftAlt.Left, rightNorm).Normalize(),
                    new Seq(leftAlt.Right, rightNorm).Normalize());
            }
            if (rightNorm is Alt rightAlt)
            {
                // X (Y | Z) => X Y | X Z
                return new Alt(
                    new Seq(leftNorm, rightAlt.Left).Normalize(),
                    new Seq(leftNorm, rightAlt.Right).Normalize());
            }
            return new Seq(leftNorm, rightNorm);
        }
    }

    /// <summary>
    /// Represents a repetition between an interval of times.
    /// </summary>
    public sealed record Rep : EbnfAst
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
        /// Initializes a new instance of the <see cref="Rep"/> class.
        /// </summary>
        /// <param name="element">The construct to repeat.</param>
        /// <param name="min">The minimum amount of repetitions.</param>
        /// <param name="max">The maximum amount of repetitions. Null, if unbounded.</param>
        public Rep(EbnfAst element, int min, int? max)
        {
            if (min < 0) throw new ArgumentOutOfRangeException(nameof(min), "Min can't be less than 0.");
            if (max is not null && max.Value < min) throw new ArgumentOutOfRangeException(nameof(max), "Max can't be smaller than min.");

            this.Element = element;
            this.Min = min;
            this.Max = max;
        }

        /// <inheritdoc/>
        public override EbnfAst Normalize()
        {
            var elementNorm = this.Element.Normalize();
            // If already normalized, don't bother further
            if (this.Min == 0 && this.Max is null) return new Rep(elementNorm, 0, null);

            // Construct the minimum required
            EbnfAst result = Epsilon.Instance;
            if (this.Min > 0)
            {
                result = elementNorm;
                for (var i = 1; i < this.Min; ++i) result = new Seq(result, elementNorm);
            }

            // No upper bound, just add a 0-or-more at the end
            if (this.Max is null) return new Seq(result, new Rep(elementNorm, 0, null)).Normalize();

            // There's an upper bound, need to append optional elements
            // We solve that by generating a sequence of (Element | Epsilon)
            var opt = new Alt(elementNorm, Epsilon.Instance);
            for (var i = this.Min + 1; i <= this.Max.Value; ++i) result = new Seq(result, opt);

            // We are done
            return result.Normalize();
        }
    }
}
