using SynKit.Collections;
using SynKit.Grammar.ContextFree.Internal;
using System.Collections.Immutable;
using System.Text;

namespace SynKit.Grammar.ContextFree;

/// <summary>
/// Represents a context-free grammar with a set of production rules.
/// </summary>
public sealed partial class CfGrammar
{
    /// <summary>
    /// Parses a context-free grammar from text.
    /// </summary>
    /// <param name="reader">The reader to read the text from.</param>
    /// <returns>The parsed <see cref="CfGrammar"/>.</returns>
    public static CfGrammar Parse(TextReader reader) => new CfParser(new CfLexer(reader)).Parse();

    /// <summary>
    /// Parses a context-free grammar from text.
    /// </summary>
    /// <param name="text">The text to parse from.</param>
    /// <returns>The parsed <see cref="CfGrammar"/>.</returns>
    public static CfGrammar Parse(string text) => Parse(new StringReader(text));

    /// <summary>
    /// All terminals in this grammar.
    /// </summary>
    public IReadOnlySet<Symbol.Terminal> Terminals => this.terminals;

    /// <summary>
    /// All nonterminals in this grammar.
    /// </summary>
    public IReadOnlySet<Symbol.Nonterminal> Nonterminals => this.nonterminals;

    /// <summary>
    /// All productions in this grammar.
    /// </summary>
    public IEnumerable<Production> Productions => this.productions.Values.SelectMany(x => x);

    private readonly HashSet<Symbol.Terminal> terminals = new() { Symbol.Terminal.EndOfInput };
    private readonly HashSet<Symbol.Nonterminal> nonterminals = new() { Symbol.Nonterminal.Start };
    private readonly Dictionary<Symbol.Nonterminal, HashSet<Production>> productions = new();

    /// <inheritdoc/>
    public override string ToString()
    {
        var result = new StringBuilder();
        foreach (var (left, rightList) in this.productions)
        {
            if (rightList.Count == 0) continue;

            var leftWithArrow = $"{left} -> ";
            result.Append(leftWithArrow);
            var first = true;
            foreach (var right in rightList.Select(p => p.Right))
            {
                if (!first) result.Append(' ', leftWithArrow.Length - 2).Append("| ");
                first = false;
                if (right.Count == 0) result.Append('ε');
                else result.AppendJoin(" ", right);
                result.AppendLine();
            }
        }
        return result.ToString();
    }

    /// <summary>
    /// Gets the production rules for a given nonterminal.
    /// </summary>
    /// <param name="nonterminal">The nonterminal to get the production rules for.</param>
    /// <returns>The production rules, where <paramref name="nonterminal"/> is on the left.</returns>
    public IReadOnlySet<Production> GetProductions(Symbol.Nonterminal nonterminal) =>
        this.productions.TryGetValue(nonterminal, out var productions)
        ? productions
        : ImmutableHashSet<Production>.Empty;

    /// <summary>
    /// Adds a production rule to this grammar.
    /// </summary>
    /// <param name="production">The production to add.</param>
    public void AddProduction(Production production)
    {
        // The cached things become invalid
        this.InvalidateCache();

        // Add to productions
        if (!this.productions.TryGetValue(production.Left, out var rightList))
        {
            rightList = new();
            this.productions.Add(production.Left, rightList);
        }
        rightList.Add(production);

        // Add all symbols
        this.nonterminals.Add(production.Left);
        foreach (var s in production.Right)
        {
            if (Symbol.Nonterminal.Start.Equals(s)) throw new ArgumentException("The start symbol can't appear on the right side of a production.", nameof(production));
            _ = s switch
            {
                Symbol.Terminal t => this.terminals.Add(t),
                Symbol.Nonterminal nt => this.nonterminals.Add(nt),
                Symbol.Epsilon => throw new ArgumentException("An epsilon symbol is invalid in a production, use a production with no symbols on the right instead.", nameof(production)),
                _ => throw new ArgumentException($"Unknown symbol {s} in production.", nameof(production)),
            };
        }
    }

    /// <summary>
    /// Checks if the given nonterminal is nullable, meaning that the empty word can be derived from it.
    /// </summary>
    /// <param name="nonterminal">The nonterminal to check.</param>
    /// <returns>True, if <paramref name="nonterminal"/> is nullable, false otherwise.</returns>
    public bool IsNullable(Symbol.Nonterminal nonterminal)
    {
        this.nullables ??= this.CalculateNullables();
        return this.nullables.Contains(nonterminal);
    }

    /// <summary>
    /// Retrieves the first set of a given symbol, which is the first terminal or empty word that can be
    /// derived from it.
    /// </summary>
    /// <param name="symbol">The symbol to get the first set of.</param>
    /// <returns>The first set of <paramref name="symbol"/>.</returns>
    public IReadOnlySet<Symbol> FirstSet(Symbol symbol)
    {
        if (symbol is not Symbol.Nonterminal
         && symbol is not Symbol.Terminal) throw new ArgumentOutOfRangeException(nameof(symbol), "Only terminals and nonterminals can have first sets.");
        this.firstSets ??= this.CalculateFirstSets();
        return this.firstSets[symbol];
    }

    /// <summary>
    /// Retrieves the first set of a symbol sequence.
    /// </summary>
    /// <param name="symbolSequence">The symbol sequence to retrieve the first set of.</param>
    /// <returns>The first set of <paramref name="symbolSequence"/>.</returns>
    public IReadOnlySet<Symbol> FirstSet(IEnumerable<Symbol> symbolSequence)
    {
        var first = new HashSet<Symbol>();
        var derivesEpsilon = true;

        foreach (var symbol in symbolSequence)
        {
            var firstSym = this.FirstSet(symbol);
            foreach (var item in firstSym) first.Add(item);
            if (!firstSym.Contains(Symbol.Epsilon.Instance))
            {
                derivesEpsilon = false;
                break;
            }
        }

        if (derivesEpsilon) first.Add(Symbol.Epsilon.Instance);
        else first.Remove(Symbol.Epsilon.Instance);

        return first;
    }

    /// <summary>
    /// Retrieves the follow set of a given nonterminal, which is the first terminal that can appear after it in the
    /// grammar.
    /// </summary>
    /// <param name="nonterminal">The symbol to get the first set of.</param>
    /// <returns>The first set of <paramref name="nonterminal"/>.</returns>
    public IReadOnlySet<Symbol.Terminal> FollowSet(Symbol.Nonterminal nonterminal)
    {
        this.followSets ??= this.CalculateFollowSets();
        return this.followSets[nonterminal];
    }

    /// <summary>
    /// Generates sequences of terminals that are accepted by this grammar.
    /// </summary>
    /// <returns>A potentially infinite sequence of <see cref="Symbol.Terminal"/>s, that are accepted.</returns>
    public IEnumerable<IReadOnlyList<Symbol.Terminal>> GenerateSentences()
    {
        // Find the initial productions
        var initials = this.GetProductions(Symbol.Nonterminal.Start);
        // Add them to the touched set
        var touched = initials
            .Select(p => p.Right)
            .ToHashSet<IReadOnlyList<Symbol>>(EqualityComparerUtils.SequenceEqualityComparer<Symbol>());
        // Also add them to the process queue
        var queue = new PriorityQueue<IReadOnlyList<Symbol>, int>();
        foreach (var t in touched) queue.Enqueue(t, t.Count(s => s is Symbol.Nonterminal));

        // While there's something to dequeue, process it
        while (queue.TryDequeue(out var symbols, out var prio))
        {
            // If all of the symbols are terminals, no further processing is needed
            if (symbols.All(s => s is Symbol.Terminal))
            {
                yield return symbols.OfType<Symbol.Terminal>().ToList();
                continue;
            }
            // Otherwise we need to try all different substitutions
            for (var i = 0; i < symbols.Count; ++i)
            {
                // Skip terminals, they are already substituted
                if (symbols[i] is not Symbol.Nonterminal nt) continue;

                // We need to look at all production rules for this terminal and substitute it
                var productions = this.GetProductions(nt);
                foreach (var (_, right) in productions)
                {
                    // Copy, substitute
                    var copy = symbols.ToList();
                    copy.RemoveAt(i);
                    copy.InsertRange(i, right);
                    // If new, enqueue for processing
                    if (touched.Add(copy)) queue.Enqueue(copy, prio + right.Count(s => s is Symbol.Nonterminal));
                }
            }
        }
    }
}
