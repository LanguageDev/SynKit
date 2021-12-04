namespace SynKit.Grammar.ContextFree;

partial class CfGrammar
{
    private HashSet<Symbol.Nonterminal>? nullables;
    private Dictionary<Symbol, HashSet<Symbol>>? firstSets;
    private Dictionary<Symbol.Nonterminal, HashSet<Symbol.Terminal>>? followSets;

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
        var result = new Dictionary<Symbol.Nonterminal, HashSet<Symbol.Terminal>>();

        // Initialize with an empty set
        foreach (var nt in this.Nonterminals) result.Add(nt, new());

        // Add $ to FOLLOW(START)
        result[Symbol.Nonterminal.Start].Add(Symbol.Terminal.EndOfInput);

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
