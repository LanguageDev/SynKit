using SynKit.Grammar.Ebnf;
using SynKit.Grammar.Lr;

var ebnf = new EbnfGrammar();
ebnf.StartRule = "program";
ebnf.Rules["program"] = new EbnfAst.Rep0(new EbnfAst.Rule("stmt"));
ebnf.Rules["stmt"] = new EbnfAst.Alt(new EbnfAst.Term("X"), new EbnfAst.Term("Y"));

var cfg = ebnf.ToContextFreeGrammar();

Console.WriteLine(cfg);
Console.WriteLine("==================");

var lalrTable = LrParsingTable.Lalr(cfg);
Console.WriteLine(lalrTable.ToDotDfa());
