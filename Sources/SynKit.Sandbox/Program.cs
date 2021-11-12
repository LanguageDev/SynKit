

using SynKit.Grammar;

var g = new ContextFreeGrammar();
g.AddProduction(new(new("S"), new[] { new Symbol.Terminal("a"), new Symbol.Terminal("b") }));
g.AddProduction(new(new("S"), Array.Empty<Symbol>()));

Console.WriteLine(g);
Console.WriteLine();
