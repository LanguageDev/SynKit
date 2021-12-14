using Scriban;
using Scriban.Runtime;

namespace SynKit.Cli.Templating;

public static class TemplateContextSetup
{
    public static void Setup(this TemplateContext context)
    {
        var scriptObject1 = new ScriptObject();
        scriptObject1.Import(typeof(UtilsInterface));
        scriptObject1.Import(typeof(LrInterface));
        context.PushGlobal(scriptObject1);
        context.TemplateLoader = new DiskTemplateLoader("Templates");
    }
}
