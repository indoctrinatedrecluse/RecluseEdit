using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Extensions.Laravel.Providers;

/// <summary>
/// Provides inline autocomplete suggestions and snippets for Laravel Blade templates,
/// control directives, authentication states, Vite assets, and Livewire components.
/// </summary>
public class BladeCompletionProvider : IInlineCompletionProvider
{
    public string Id => "laravel.blade.completion";
    public string Name => "Laravel Blade Template Directives";

    public IReadOnlyList<string> SupportedLanguages => ["blade", "php", "html"];

    private static readonly Dictionary<string, string> Completions = new()
    {
        // Layout & View Directives
        {
            "@extends(",
            "'layouts.app')"
        },
        {
            "@section(",
            "'content')\n    \n@endsection"
        },
        {
            "@yield(",
            "'content', 'Default Content')"
        },
        {
            "@include(",
            "'partials.navbar')"
        },
        {
            "@props(",
            "['title' => 'Default', 'status' => null])"
        },
        {
            "@push(",
            "'scripts')\n    <script src=\"\"></script>\n@endpush"
        },
        {
            "@stack(",
            "'scripts')"
        },

        // Control Flow Directives
        {
            "@if (",
            "$condition)\n    \n@endif"
        },
        {
            "@unless (",
            "$condition)\n    \n@endunless"
        },
        {
            "@isset(",
            "$record)\n    \n@endisset"
        },
        {
            "@empty(",
            "$records)\n    \n@endempty"
        },
        {
            "@foreach (",
            "$items as $item)\n    <div>{{ $item }}</div>\n@endforeach"
        },
        {
            "@forelse (",
            "$items as $item)\n    <div>{{ $item }}</div>\n@empty\n    <p>No items found.</p>\n@endforelse"
        },

        // Security, Forms & Validation
        {
            "@csrf",
            ""
        },
        {
            "@method(",
            "'PUT')"
        },
        {
            "@error(",
            "'fieldName')\n    <div class=\"text-red-500 text-sm\">{{ $message }}</div>\n@enderror"
        },

        // Auth & Environment
        {
            "@auth",
            "\n    \n@endauth"
        },
        {
            "@guest",
            "\n    \n@endguest"
        },

        // Modern Tooling & Livewire
        {
            "@vite(",
            "['resources/css/app.css', 'resources/js/app.js'])"
        },
        {
            "@livewire(",
            "'component-name')"
        },
        {
            "{{-- ",
            "Blade Comment --}}"
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
            if (prefix.EndsWith(trigger, StringComparison.Ordinal))
            {
                return Task.FromResult<string?>(suggestion);
            }
        }

        return Task.FromResult<string?>(null);
    }
}
