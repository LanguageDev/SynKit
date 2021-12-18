using Scriban;
using Scriban.Runtime;
using SynKit.Cli.Templating;
using SynKit.Collections;
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

        var context = new TemplateContext();
        context.Setup();
        var table = lalrtable;
        var scriptObject1 = new ScriptObject();
        scriptObject1.Add("table", table);

        context.PushGlobal(scriptObject1);
        context.TemplateLoader = new DiskTemplateLoader("Templates");

#if false
        // C# code
        {
            var template = Template.Parse(File.ReadAllText("Templates/CSharp/lr_parser.template"));
            var result = template.Render(context);

            Console.WriteLine(result);
            //File.WriteAllText("table.html", result);
        }
        // Table
        {
            var template = Template.Parse(File.ReadAllText("Templates/Visualization/lr_parsing_table.template"));
            var result = template.Render(context);

            File.WriteAllText("table.html", result);
        }
#else
        var template = Template.Parse(@"
{{-
include 'utils.template'
$a = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13]
-}}
{{-wrap chunked $a 4-}}
    {{-wrap join $chunk ', ' !$last-}}
        {{-$element}}
    {{-end}}
{{end}}
");
        var result = template.Render(context);

        Console.WriteLine(result);
#endif
    }

    static void TableStats(ILrParsingTable table)
    {
        Console.WriteLine($"states: {table.States.Count}");
        Console.WriteLine($"action table size: {table.States.Count * table.Terminals.Count}");
        var emptyT = table.Terminals.Sum(t => table.States.Count(s => table.Action[s, t].Count == 0));
        Console.WriteLine($"    of that empty: {emptyT} ({emptyT / (float)(table.States.Count * table.Terminals.Count)})");
        Console.WriteLine($"    COMPRESSED: {RleCompressAction(table)}");
        Console.WriteLine($"    COMPRESSED DEDUP ROWS: {RleCompressActionDedup(table)}");
        Console.WriteLine($"goto table size: {table.States.Count * table.Nonterminals.Count}");
        var emptyNt = table.Nonterminals.Sum(nt => table.States.Count(s => table.Goto[s, nt] is null));
        Console.WriteLine($"    of that empty: {emptyNt} ({emptyNt / (float)(table.States.Count * table.Nonterminals.Count)})");
    }

    static int RleCompressAction(ILrParsingTable table) => table.States
        .SelectMany(state => table.Terminals.Select(term => table.Action[state, term]))
        .RunLengthEncode(EqualityComparerUtils.SetEqualityComparer<LrAction>())
        .Count();

    static int RleCompressActionDedup(ILrParsingTable table) => DedupRows(table)
        .SelectMany(x => x)
        .RunLengthEncode(EqualityComparerUtils.SetEqualityComparer<LrAction>())
        .Count();

    static IEnumerable<IEnumerable<ICollection<LrAction>>> DedupRows(ILrParsingTable table)
    {
        var rows = new HashSet<IEnumerable<ICollection<LrAction>>>(
            EqualityComparerUtils.SequenceEqualityComparer(EqualityComparerUtils.SetEqualityComparer<LrAction>()));
        foreach (var state in table.States)
        {
            var row = table.Terminals.Select(t => table.Action[state, t]);
            if (rows.Add(row)) yield return row;
        }
    }
}
