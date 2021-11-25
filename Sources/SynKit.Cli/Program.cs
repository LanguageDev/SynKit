
using Scriban;
using Scriban.Runtime;
using SynKit.Grammar.Cfg;
using SynKit.Grammar.Lr;
using SynKit.Grammar.Lr.Tables;

internal class CliOptions
{
}

internal class LrTableInterface
{
    public static IEnumerable<LrState> AllStates(ILrParsingTable table) => table.StateItemSets.Select(i => i.State);

    public static LrAction? Action(ILrParsingTable table, LrState state, Symbol.Terminal terminal)
    {
        var actions = table.Action[state, terminal];
        if (actions.Count == 0) return null;
        if (actions.Count == 1) return actions.First();
        var shiftAction = actions.OfType<LrAction.Shift>().FirstOrDefault();
        if (shiftAction is not null) return shiftAction;
        return actions.First();
    }

    public static LrState? Goto(ILrParsingTable table, LrState state, Symbol.Nonterminal nonterminal) =>
        table.Goto[state, nonterminal];
}

internal static class Program
{
    static void Main(string[] args)
    {
        var table = BuildTestTable();

        var scriptObject1 = new ScriptObject();
        scriptObject1.Add("table", table);
        scriptObject1.Import(typeof(LrTableInterface));

        var context = new TemplateContext();
        context.PushGlobal(scriptObject1);

        var template = Template.Parse(@"
{{
for state in all_states(table)
    for terminal in table.terminals
        $act = action(table, state, terminal)
        if $act == null
            continue
        end
}}
        {{ state }}, {{ terminal }}, {{ $act }}
{{
    end
end
}}
");
        var result = template.Render(context);

        // Prints This is MyFunctions.Hello: `hello from method!`
        Console.WriteLine(result);
    }

    static ILrParsingTable BuildTestTable()
    {
        var stmt = new Symbol.Nonterminal("stmt");
        var expr = new Symbol.Nonterminal("expr");
        var num = new Symbol.Nonterminal("num");

        var T_if = new Symbol.Terminal("if");
        var T_then = new Symbol.Terminal("then");
        var T_else = new Symbol.Terminal("else");
        var T_q = new Symbol.Terminal("?");
        var T_obracked = new Symbol.Terminal("[");
        var T_cbracked = new Symbol.Terminal("]");
        var T_asgn = new Symbol.Terminal(":=");
        var T_plus = new Symbol.Terminal("+");
        var T_digit = new Symbol.Terminal("digit");
        var T_arr = new Symbol.Terminal("arr");

        var cfg = new ContextFreeGrammar();
        cfg.AddProduction(new(Symbol.Nonterminal.Start, new[] { stmt }));
        cfg.AddProduction(new(stmt, new Symbol[] { T_if, expr, T_then, stmt, T_else, stmt }));
        cfg.AddProduction(new(stmt, new Symbol[] { T_if, expr, T_then, stmt }));
        cfg.AddProduction(new(stmt, new Symbol[] { expr, T_q, stmt, stmt }));
        cfg.AddProduction(new(stmt, new Symbol[] { T_arr, T_obracked, expr, T_cbracked, T_asgn, expr }));
        cfg.AddProduction(new(expr, new Symbol[] { num }));
        cfg.AddProduction(new(expr, new Symbol[] { expr, T_plus, expr }));
        cfg.AddProduction(new(num, new Symbol[] { T_digit }));
        cfg.AddProduction(new(num, new Symbol[] { num, T_digit }));

        return LrParsingTable.Lalr(cfg);
    }
}
