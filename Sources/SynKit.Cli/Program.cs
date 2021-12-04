
using Scriban;
using Scriban.Runtime;
using SynKit.Cli.Templating;
using SynKit.Grammar.ContextFree;
using SynKit.Grammar.Lr.Tables;

internal static class Program
{
    static void Main(string[] args)
    {
        var grammar = CfGrammar.Parse(@"
E -> E + T | T
T -> T * F | F
F -> n | (E)
");
        var table = LrParsingTable.Lr0(grammar);

        var scriptObject1 = new ScriptObject();
        scriptObject1.Add("table", table);
        scriptObject1.Import(typeof(UtilsInterface));
        scriptObject1.Import(typeof(LrInterface));

        var context = new TemplateContext();
        context.PushGlobal(scriptObject1);
        context.TemplateLoader = new DiskTemplateLoader("Templates");

        var template = Template.Parse(File.ReadAllText("Templates/lr_parsing_table.template"));
        var result = template.Render(context);

        Console.WriteLine(result);
        File.WriteAllText("table.html", result);
    }

    static ILrParsingTable BuildTestTable2()
    {
        var stmt = new Symbol.Nonterminal("stmt");

        var T_a = new Symbol.Terminal("a");

        var cfg = new CfGrammar();
        cfg.AddProduction(new(Symbol.Nonterminal.Start, new[] { stmt }));
        cfg.AddProduction(new(stmt, new Symbol[] { }));
        cfg.AddProduction(new(stmt, new Symbol[] { stmt, T_a }));

        return LrParsingTable.Lr0(cfg);
    }

    static ILrParsingTable BuildTestTable()
    {
        var expr = new Symbol.Nonterminal("expr");
        var factor = new Symbol.Nonterminal("factor");
        var atom = new Symbol.Nonterminal("atom");

        var T_opaern = new Symbol.Terminal("(");
        var T_cpaern = new Symbol.Terminal(")");
        var T_plus = new Symbol.Terminal("+");
        var T_star = new Symbol.Terminal("*");
        var T_num = new Symbol.Terminal("num");

        var cfg = new CfGrammar();
        cfg.AddProduction(new(Symbol.Nonterminal.Start, new[] { expr }));
        cfg.AddProduction(new(expr, new Symbol[] { expr, T_plus, factor }));
        cfg.AddProduction(new(expr, new Symbol[] { factor }));
        cfg.AddProduction(new(factor, new Symbol[] { factor, T_star, atom }));
        cfg.AddProduction(new(factor, new Symbol[] { atom }));
        cfg.AddProduction(new(atom, new Symbol[] { T_opaern, expr, T_cpaern }));
        cfg.AddProduction(new(atom, new Symbol[] { T_num }));

        return LrParsingTable.Lalr(cfg);
    }
}
