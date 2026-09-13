using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Extensions.Frontend.Providers;

/// <summary>
/// Provides inline ghost-text completions for Astro components,
/// frontmatter script blocks, props, slots, and client hydration directives.
/// </summary>
public class AstroCompletionProvider : IInlineCompletionProvider
{
    public string Id => "frontend.astro.inline";
    public string Name => "Astro Component & Frontmatter Templates";

    public IReadOnlyList<string> SupportedLanguages => ["astro"];

    private static readonly Dictionary<string, string> Completions = new(StringComparer.OrdinalIgnoreCase)
    {
        {
            "---",
            "\ninterface Props {\n  title: string;\n  description?: string;\n}\n\nconst { title, description = 'Default description' } = Astro.props;\n---\n\n<article class=\"astro-card\">\n  <h2>{title}</h2>\n  <p>{description}</p>\n  <slot />\n</article>\n\n<style>\n  .astro-card {\n    padding: 1.5rem;\n    border-radius: 8px;\n  }\n</style>"
        },
        {
            "interface Props {",
            "\n  title: string;\n  count?: number;\n}\n\nconst { title, count = 0 } = Astro.props;"
        },
        {
            "const { ",
            "title, children } = Astro.props;"
        },
        {
            "<slot",
            " name=\"header\" />"
        },
        {
            "client:load",
            "={true}"
        },
        {
            "client:visible",
            "={true}"
        }
    };

    public Task<string?> GetInlineSuggestionAsync(InlineCompletionContext context, CancellationToken cancellationToken = default)
    {
        var line = context.CurrentLineText;
        var col = context.ColumnNumber - 1;
        if (col < 0 || col > line.Length) col = line.Length;

        var prefix = line[..col];
        if (string.IsNullOrWhiteSpace(prefix)) return Task.FromResult<string?>(null);

        foreach (var (trigger, suggestion) in Completions)
        {
            if (prefix.EndsWith(trigger, StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult<string?>(suggestion);
            }
        }

        return Task.FromResult<string?>(null);
    }
}

