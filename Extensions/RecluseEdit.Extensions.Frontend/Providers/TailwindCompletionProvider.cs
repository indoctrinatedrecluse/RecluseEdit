using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Extensions.Frontend.Providers;

/// <summary>
/// Provides inline ghost-text completions and snippets for Tailwind CSS (v3 and v4),
/// including responsive/state modifiers, utility classes, and CSS directives (@theme, @utility, @apply, @tailwind).
/// </summary>
public class TailwindCompletionProvider : IInlineCompletionProvider
{
    public string Id => "frontend.tailwind.inline";
    public string Name => "Tailwind CSS v3/v4 & Modern Utility Styling";

    public IReadOnlyList<string> SupportedLanguages =>
    [
        "html",
        "css",
        "vue",
        "svelte",
        "astro",
        "javascript",
        "typescript",
        "jsx",
        "tsx",
        "razor",
        "cshtml"
    ];

    private static readonly Dictionary<string, string> Completions = new(StringComparer.OrdinalIgnoreCase)
    {
        // Tailwind v4 & v3 CSS Directives
        { "@theme", " {\n  --color-primary: #6366f1;\n  --color-secondary: #ec4899;\n  --font-display: 'Inter', sans-serif;\n}" },
        { "@utility ", "btn-primary {\n  @apply inline-flex items-center justify-center px-4 py-2 font-medium rounded-lg bg-indigo-600 text-white hover:bg-indigo-500 shadow-sm transition-colors;\n}" },
        { "@apply ", "flex items-center justify-between px-4 py-2 rounded-lg bg-slate-800 text-white;" },
        { "@tailwind ", "base;\n@tailwind components;\n@tailwind utilities;" },
        { "@custom-variant ", "dark (&:where([data-theme=dark], [data-theme=dark] *));" },
        { "@layer components", " {\n  .card {\n    @apply rounded-xl border border-slate-700 bg-slate-800/80 p-6 shadow-lg backdrop-blur;\n  }\n}" },
        { "@layer utilities", " {\n  .content-auto {\n    content-visibility: auto;\n  }\n}" },

        // Common Layout & Flexbox/Grid Snippets
        { "class=\"flex ", "items-center justify-between gap-4\"" },
        { "className=\"flex ", "items-center justify-between gap-4\"" },
        { "class=\"grid ", "grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6\"" },
        { "className=\"grid ", "grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6\"" },
        { "class=\"container ", "mx-auto px-4 sm:px-6 lg:px-8\"" },
        { "className=\"container ", "mx-auto px-4 sm:px-6 lg:px-8\"" },
        { "class=\"btn ", "inline-flex items-center justify-center gap-2 px-4 py-2 text-sm font-semibold rounded-lg bg-indigo-600 text-white hover:bg-indigo-500 transition-colors shadow-sm\"" },
        { "className=\"btn ", "inline-flex items-center justify-center gap-2 px-4 py-2 text-sm font-semibold rounded-lg bg-indigo-600 text-white hover:bg-indigo-500 transition-colors shadow-sm\"" },
        { "class=\"card ", "rounded-2xl border border-slate-800 bg-slate-900/60 p-6 shadow-xl backdrop-blur-md\"" },
        { "className=\"card ", "rounded-2xl border border-slate-800 bg-slate-900/60 p-6 shadow-xl backdrop-blur-md\"" },

        // Utility Class Prefixes
        { "items-", "center justify-between" },
        { "justify-", "between items-center" },
        { "bg-slate-", "900 text-slate-100" },
        { "bg-indigo-", "600 hover:bg-indigo-500 text-white" },
        { "text-slate-", "400 text-sm" },
        { "rounded-", "xl shadow-md border border-slate-700" },
        { "shadow-", "xl border border-slate-800" },
        { "hover:bg-", "slate-800 transition-colors" },
        { "dark:bg-", "slate-950 dark:text-slate-50" },
        { "transition-", "all duration-200 ease-in-out" },
        { "animate-", "pulse" }
    };

    public Task<string?> GetInlineSuggestionAsync(InlineCompletionContext context, CancellationToken cancellationToken = default)
    {
        var prefix = !string.IsNullOrEmpty(context.TextBeforeCaret)
            ? context.TextBeforeCaret
            : context.CurrentLineText;

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
