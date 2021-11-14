using System.Collections.Immutable;
using System.Text;

namespace SynKit.Grammar.Cfg;

/// <summary>
/// Represents a context-free grammar with a set of production rules.
/// </summary>
public sealed class ContextFreeGrammar
{
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

    /// <summary>
    /// The start symbol of the grammar.
    /// </summary>
    public Symbol.Nonterminal? StartSymbol
    {
        get => this.startSymbol;
        set
        {
            this.InvalidateCache();
            this.startSymbol = value;
        }
    }

    private readonly HashSet<Symbol.Terminal> terminals = new() { Symbol.Terminal.EndOfInput };
    private readonly HashSet<Symbol.Nonterminal> nonterminals = new();
    private readonly Dictionary<Symbol.Nonterminal, HashSet<Production>> productions = new();

    private Symbol.Nonterminal? startSymbol;
    private Dictionary<Symbol, HashSet<Symbol>>? firstSets;
    private Dictionary<Symbol.Nonterminal, HashSet<Symbol.Terminal>>? followSets;
    private HashSet<Symbol.Nonterminal>? nullables;

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
    /// Augments the start symbol, meaning that it is replaced with a new, fresh symbol, so it is not used recursively.
    /// </summary>
    public void AugmentStartSymbol()
    {
        if (this.StartSymbol is null) throw new InvalidOperationException("Can't augment the start symbol without specifying it.");

        var oldStart = this.StartSymbol;
        this.StartSymbol = this.StartSymbol.Fresh();
        this.AddProduction(new(this.StartSymbol, new[] { oldStart }));
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
            foreach (var item in firstSym.OfType<Symbol.Terminal>()) first.Add(item);
            if (!firstSym.Contains(Symbol.Epsilon.Instance))
            {
                derivesEpsilon = false;
                break;
            }
        }

        if (derivesEpsilon) first.Add(Symbol.Epsilon.Instance);

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

    private void InvalidateCache()
    {
        this.firstSets = null;
        this.followSets = null;
        this.nullables = null;
    }

    private HashSet<Symbol.Nonterminal> CalculateNullables()
    {
        var result = new HashSet<Symbol.Nonterminal>();
        // Initially we add all left-side of productions that has an empty right-side
        foreach (var prod in this.Productions.Where(p => p.Right.Count == 0)) result.Add(prod.Left);
        // While there is a change, we refine the set
        while (true)
        {
            var change = false;

            foreach (var prod in this.Productions)
            {
                // If the productions right side consists of only nullables, we add its left side
                if (prod.Right.Any(s => s is not Symbol.Nonterminal nt || !result.Contains(nt))) continue;
                // It is nullable
                change = result.Add(prod.Left) || change;
            }

            if (!change) break;
        }
        return result;
    }

    private Dictionary<Symbol, HashSet<Symbol>> CalculateFirstSets()
    {
        var result = new Dictionary<Symbol, HashSet<Symbol>>
        {
            // Special case, # is not in the grammar
            [Symbol.Terminal.NotInGrammar] = new() { Symbol.Terminal.NotInGrammar }
        };

        // For all terminals X, FIRST(X) = { X }
        foreach (var t in this.Terminals) result[t] = new() { t };

        // For all nonterminals we simply initialize with an empty set
        foreach (var nt in this.Nonterminals) result[nt] = new();

        // While there is change, we refine the sets
        while (true)
        {
            var change = false;

            // Go through each production
            foreach (var (left, right) in this.Productions)
            {
                var leftFirstSet = result[left];
                var producesEpsilon = true;

                // Go through each symbol in the production
                foreach (var sym in right)
                {
                    // All terminals of FIRST(sym) belongs into FIRST(left)
                    var symFirstSet = result[sym];
                    foreach (var t in symFirstSet.OfType<Symbol.Terminal>()) change = leftFirstSet.Add(t) || change;

                    // If FIRST(sym) does not produce epsilon, the chain is broken
                    if (!symFirstSet.Contains(Symbol.Epsilon.Instance))
                    {
                        producesEpsilon = false;
                        break;
                    }
                }

                // If all produced epsilon, left does too
                if (producesEpsilon) change = leftFirstSet.Add(Symbol.Epsilon.Instance) || change;
            }

            if (!change) break;
        }

        return result;
    }

    private Dictionary<Symbol.Nonterminal, HashSet<Symbol.Terminal>> CalculateFollowSets()
    {
        if (this.StartSymbol is null) throw new InvalidOperationException("Start symbol is not set for the grammar!");
        var result = new Dictionary<Symbol.Nonterminal, HashSet<Symbol.Terminal>>();

        // Initialize with an empty set
        foreach (var nt in this.Nonterminals) result.Add(nt, new());

        // Add $ to FOLLOW(S)
        result[this.StartSymbol].Add(Symbol.Terminal.EndOfInput);

        // While there is change, we refine the sets
        while (true)
        {
            var change = false;

            // Go through each production
            foreach (var (left, right) in this.Productions)
            {
                var leftFollowSet = result[left];

                // Go through each symbol of right
                for (var i = 0; i < right.Count; ++i)
                {
                    // We only care about nonterminals
                    if (right[i] is not Symbol.Nonterminal nt) continue;

                    // Anything in FIRST(remaining) will be in FOLLOW(nt)
                    var ntSet = result[nt];
                    var remaining = this.FirstSet(right.Skip(i + 1));
                    foreach (var item in remaining.OfType<Symbol.Terminal>()) change = ntSet.Add(item) || change;

                    // If FIRST(remaining) produced the empty word, we add everything in FOLLOW(left) to FOLLOW(nt)
                    if (remaining.Contains(Symbol.Epsilon.Instance))
                    {
                        foreach (var item in leftFollowSet) change = ntSet.Add(item) || change;
                    }
                }
            }

            if (!change) break;
        }

        return result;
    }
}
