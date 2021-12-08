using Scriban;
using Scriban.Runtime;
using SynKit.Cli.Templating;
using SynKit.Grammar.Ebnf;
using SynKit.Grammar.Lr;
using SynKit.Grammar.Lr.Tables;

namespace SynKit.Cli;

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

        //var lr0table = LrParsingTable.Lr0(cfGrammar);
        //var slrtable = LrParsingTable.Slr(cfGrammar);
        var lalrtable = LrParsingTable.Lalr(cfGrammar);
        //var clrtable = LrParsingTable.Clr(cfGrammar);

        //TableStats(lr0table);
        //TableStats(slrtable);
        //TableStats(lalrtable);
        //TableStats(clrtable);

        //return;

        var table = lalrtable;
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

    static void TableStats(ILrParsingTable table)
    {
        Console.WriteLine($"states: {table.States.Count}");
        Console.WriteLine($"action table size: {table.States.Count * table.Terminals.Count}");
        var emptyT = table.Terminals.Sum(t => table.States.Count(s => table.Action[s, t].Count == 0));
        Console.WriteLine($"    of that empty: {emptyT} ({emptyT / (float)(table.States.Count * table.Terminals.Count)})");
        Console.WriteLine($"goto table size: {table.States.Count * table.Nonterminals.Count}");
        var emptyNt = table.Nonterminals.Sum(nt => table.States.Count(s => table.Goto[s, nt] is null));
        Console.WriteLine($"    of that empty: {emptyNt} ({emptyNt / (float)(table.States.Count * table.Nonterminals.Count)})");
    }
}
