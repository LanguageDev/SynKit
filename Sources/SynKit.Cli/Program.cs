
using Scriban;
using Scriban.Runtime;
using SynKit.Cli.Templating;
using SynKit.Grammar.ContextFree;
using SynKit.Grammar.Ebnf;
using SynKit.Grammar.Lr.Tables;

internal static class Program
{
    static void Main(string[] args)
    {
        var ebnfGrammar = EbnfGrammar.Parse(EbnfFlavor.Standard, @"
	chunk ::= block

	block ::= {stat} [retstat]

	stat ::=  ‘;’ | 
		 varlist ‘=’ explist | 
		 functioncall | 
		 label | 
		 break | 
		 goto Name | 
		 do block end | 
		 while exp do block end | 
		 repeat block until exp | 
		 if exp then block {elseif exp then block} [else block] end | 
		 for Name ‘=’ exp ‘,’ exp [‘,’ exp] do block end | 
		 for namelist in explist do block end | 
		 function funcname funcbody | 
		 local function Name funcbody | 
		 local namelist [‘=’ explist] 

	retstat ::= return [explist] [‘;’]

	label ::= ‘::’ Name ‘::’

	funcname ::= Name {‘.’ Name} [‘:’ Name]

	varlist ::= var {‘,’ var}

	var ::=  Name | prefixexp ‘[’ exp ‘]’ | prefixexp ‘.’ Name 

	namelist ::= Name {‘,’ Name}

	explist ::= exp {‘,’ exp}

	exp ::=  nil | false | true | Numeral | LiteralString | ‘...’ | functiondef | 
		 prefixexp | tableconstructor | exp binop exp | unop exp 

	prefixexp ::= var | functioncall | ‘(’ exp ‘)’

	functioncall ::=  prefixexp args | prefixexp ‘:’ Name args 

	args ::=  ‘(’ [explist] ‘)’ | tableconstructor | LiteralString 

	functiondef ::= function funcbody

	funcbody ::= ‘(’ [parlist] ‘)’ block end

	parlist ::= namelist [‘,’ ‘...’] | ‘...’

	tableconstructor ::= ‘{’ [fieldlist] ‘}’

	fieldlist ::= field {fieldsep field} [fieldsep]

	field ::= ‘[’ exp ‘]’ ‘=’ exp | Name ‘=’ exp | exp

	fieldsep ::= ‘,’ | ‘;’

	binop ::=  ‘+’ | ‘-’ | ‘*’ | ‘/’ | ‘//’ | ‘^’ | ‘%’ | 
		 ‘&’ | ‘~’ | ‘|’ | ‘>>’ | ‘<<’ | ‘..’ | 
		 ‘<’ | ‘<=’ | ‘>’ | ‘>=’ | ‘==’ | ‘~=’ | 
		 and | or

	unop ::= ‘-’ | not | ‘#’ | ‘~’
");
        var cfGrammar = ebnfGrammar.ToCfGrammar();

        var table = LrParsingTable.Lr0(cfGrammar);

        var scriptObject1 = new ScriptObject();
        scriptObject1.Add("table", table);
        scriptObject1.Import(typeof(UtilsInterface));
        scriptObject1.Import(typeof(LrInterface));

        var context = new TemplateContext();
        context.PushGlobal(scriptObject1);
        context.TemplateLoader = new DiskTemplateLoader("Templates");

        var template = Template.Parse(File.ReadAllText("Templates/CSharp/lr_parser.template"));
        var result = template.Render(context);

        Console.WriteLine(result);
        //File.WriteAllText("table.html", result);
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
