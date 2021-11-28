using Scriban;
using Scriban.Parsing;
using Scriban.Runtime;

namespace SynKit.Cli.Templating;

/// <summary>
/// A very simple ITemplateLoader loading directly from the disk, without any checks.
/// </summary>
public sealed class DiskTemplateLoader : ITemplateLoader
{
    private readonly string root;

    /// <summary>
    /// Initializes a new <see cref="DiskTemplateLoader"/>.
    /// </summary>
    /// <param name="root">The root directory relative to the current work-directory.</param>
    public DiskTemplateLoader(string root = ".")
    {
        this.root = root;
    }

    /// <inheritdoc/>
    public string GetPath(TemplateContext context, SourceSpan callerSpan, string templateName) =>
        Path.Combine(Environment.CurrentDirectory, root, templateName);

    /// <inheritdoc/>
    public string Load(TemplateContext context, SourceSpan callerSpan, string templatePath) =>
        File.ReadAllText(templatePath);

    /// <inheritdoc/>
    public ValueTask<string> LoadAsync(TemplateContext context, SourceSpan callerSpan, string templatePath) =>
        new(File.ReadAllTextAsync(templatePath));
}
