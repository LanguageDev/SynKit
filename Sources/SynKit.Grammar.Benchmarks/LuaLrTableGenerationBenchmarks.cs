using BenchmarkDotNet.Attributes;
using SynKit.Grammar.ContextFree;
using SynKit.Grammar.Ebnf;
using SynKit.Grammar.Lr.Tables;

namespace SynKit.Grammar.Benchmarks;

[MarkdownExporter]
public class LuaLrTableGenerationBenchmarks
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private CfGrammar grammar;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [GlobalSetup]
    public void GlobalSetup()
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
        grammar = ebnfGrammar.ToCfGrammar();
    }

    [Benchmark]
    public ILrParsingTable Lr0() => LrParsingTable.Lr0(this.grammar);

    [Benchmark]
    public ILrParsingTable Slr() => LrParsingTable.Slr(this.grammar);

    [Benchmark]
    public ILrParsingTable Lalr() => LrParsingTable.Lalr(this.grammar);

    [Benchmark]
    public ILrParsingTable Clr() => LrParsingTable.Clr(this.grammar);
}
