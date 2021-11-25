using SynKit.Grammar.Lr.Tables;
using System.Text;

namespace SynKit.Grammar.Lr;

/// <summary>
/// Utilities for visualizing the LR constructs.
/// </summary>
public static class LrVisualizer
{
    // TODO: Hacky escapes, like hard-replacing _ with dot or -> with arrow
    // We should find a better way!

    /// <summary>
    /// Creates a HTML representation of the Action-Goto table.
    /// </summary>
    /// <param name="table">The table to convert.</param>
    /// <returns>The HTML table representation of the table.</returns>
    public static string ToHtmlTable(this ILrParsingTable table)
    {
        const string border = "border: 1px solid black";
        const string doubleRight = "border-right: 3px black double";
        const string doubleDown = "border-bottom: 3px black double";
        const string center = "text-align: center";

        var result = new StringBuilder();

        // Header with Action and Goto
        result
            .AppendLine("<table style=\"width: 100%; border-collapse: collapse\">")
            .AppendLine("  <tr>")
            .AppendLine("    <th></th>")
            .AppendLine($"    <th colspan=\"{table.Terminals.Count}\">Action</th>")
            .AppendLine($"    <th colspan=\"{table.Nonterminals.Count}\">Goto</th>")
            .AppendLine("  </tr>");

        // Header with state, terminals and nonterminals
        result.AppendLine("  <tr>");
        // First the state
        result.AppendLine($"    <td style=\"{border}; {doubleRight}; {doubleDown}; {center}\">State</td>");
        // Next the terminals
        var i = 0;
        foreach (var term in table.Terminals)
        {
            ++i;
            var isLast = i == table.Terminals.Count;
            var append = isLast ? $"; {doubleRight}" : string.Empty;
            result.AppendLine($"    <td style=\"{border}; {doubleDown}; {center}{append}\">{term}</td>");
        }
        // Finally the nonterminals
        foreach (var nonterm in table.Nonterminals)
        {
            result.AppendLine($"    <td style=\"{border}; {doubleDown}; {center}\">{nonterm}</td>");
        }
        result.AppendLine("  </tr>");

        // Now we can actually print the contents state by state
        foreach (var (state, itemSet) in table.StateItemSets)
        {
            result.AppendLine("  <tr>");
            // First we print the state
            result.AppendLine($"    <td style=\"{border}; {doubleRight}; {center}\">{state.Id}</td>");
            // We print all actions with terminals
            i = 0;
            foreach (var term in table.Terminals)
            {
                ++i;
                var isLast = i == table.Terminals.Count;
                var append = isLast ? $"; {doubleRight}" : string.Empty;
                var actions = table.Action[state, term];
                result.AppendLine($"    <td style=\"{border}{append}\">{string.Join("<br>", actions)}</td>");
            }
            // We print all gotos for nonterminals
            foreach (var nonterm in table.Nonterminals)
            {
                var to = table.Goto[state, nonterm]?.Id;
                result.AppendLine($"    <td style=\"{border}\">{to}</td>");
            }
            result.AppendLine("  </tr>");
        }

        // Close table
        result.Append("</table>");

        result.Replace(" -> ", " → ");

        return result.ToString();
    }

    /// <summary>
    /// Creates the automata representation of an LR parsing table in DOT format.
    /// </summary>
    /// <param name="table">The table to convert.</param>
    /// <returns>The DOT DFA representation of the table.</returns>
    public static string ToDotDfa(this ILrParsingTable table)
    {
        var result = new StringBuilder();

        result
            .AppendLine("digraph PDA {")
            .AppendLine("  rankdir=LR;")
            .AppendLine("  node [shape=rectangle, style=rounded];");

        // Push out all states
        foreach (var (state, itemSet) in table.StateItemSets)
        {
            var setText = string.Join(@"\l", itemSet)
                .Replace(" -> ", " → ")
                .Replace(" _", " &#8226;");
            result.AppendLine($"  {state.Id}[label=\"{setText}\\l\", xlabel=<I<SUB>{state.Id}</SUB>>]");
        }

        // Transitions
        foreach (var (state, itemSet) in table.StateItemSets)
        {
            // Terminals
            foreach (var term in table.Terminals)
            {
                var toStates = table.Action[state, term]
                    .OfType<LrAction.Shift>()
                    .Select(s => s.State);
                foreach (var toState in toStates) result.AppendLine($"  {state.Id} -> {toState.Id} [label=\"{term}\"]");
            }
            // Nonterminals
            foreach (var nonterm in table.Nonterminals)
            {
                var toState = table.Goto[state, nonterm];
                if (toState is null) continue;
                result.AppendLine($"  {state.Id} -> {toState.Value.Id} [label=\"{nonterm}\"]");
            }
        }

        result.Append('}');

        result.Replace("'", "′");

        return result.ToString();
    }
}
