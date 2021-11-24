using SynKit.Grammar.Cfg;
using SynKit.Grammar.Lr;

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

Console.WriteLine(cfg);
Console.WriteLine("==================");
var lalrTable = LrParsingTable.Lalr(cfg);
Console.WriteLine(lalrTable.ToHtmlTable());
Console.WriteLine("==================");
Console.WriteLine(lalrTable.ToDotDfa());
