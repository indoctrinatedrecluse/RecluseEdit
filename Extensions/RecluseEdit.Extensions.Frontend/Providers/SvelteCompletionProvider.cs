using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Extensions.Frontend.Providers;

/// <summary>
/// Provides inline ghost-text completions for Svelte 5 components,
/// modern runes ($state, $derived, $effect, $props), and logic blocks.
/// </summary>
public class SvelteCompletionProvider : IInlineCompletionProvider
{
    public string Id => "frontend.svelte.inline";
    public string Name => "Svelte 5 Runes & Templates";

    public IReadOnlyList<string> SupportedLanguages => ["svelte"];

    private static readonly Dictionary<string, string> Completions = new(StringComparer.OrdinalIgnoreCase)
    {
        {
            "<script",
            " lang=\"ts\">\n  let { title = 'Svelte 5 App' } = $props();\n  let count = $state(0);\n  let double = $derived(count * 2);\n</script>\n\n<main>\n  <h1>{title}</h1>\n  <button on:click={() => count++}>Count: {count} (x2: {double})</button>\n</main>\n\n<style>\n  main {\n    font-family: sans-serif;\n  }\n</style>"
        },
        {
            "let count = $state(",
            "0);"
        },
        {
            "let double = $derived(",
            "count * 2);"
        },
        {
            "$effect(",
            "() => {\n  console.log('Count updated:', count);\n});"
        },
        {
            "let { ",
            "title, children } = $props();"
        },
        {
            "let value = $bindable(",
            "'');"
        },
        {
            "$inspect(",
            "count);"
        },
        {
            "{#if ",
            "condition}\n  <p>Visible</p>\n{:else}\n  <p>Hidden</p>\n{/if}"
        },
        {
            "{#each ",
            "items as item (item.id)}\n  <li>{item.name}</li>\n{/each}"
        },
        {
            "{#await ",
            "fetchData()}\n  <p>Loading...</p>\n{:then data}\n  <p>{data}</p>\n{:catch error}\n  <p>{error.message}</p>\n{/await}"
        },
        {
            "bind:value={",
            "val}"
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

