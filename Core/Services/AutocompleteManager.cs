using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Core.Services;

/// <summary>
/// Aggregates and coordinates inline ghost-text and IntelliSense completion providers.
/// </summary>
public class AutocompleteManager
{
    private readonly List<IInlineCompletionProvider> _inlineProviders = [];
    private readonly List<IIntelliSenseProvider> _intelliSenseProviders = [];
    private readonly object _lock = new();

    public IReadOnlyList<IInlineCompletionProvider> InlineProviders
    {
        get
        {
            lock (_lock) return _inlineProviders.ToList();
        }
    }

    public IReadOnlyList<IIntelliSenseProvider> IntelliSenseProviders
    {
        get
        {
            lock (_lock) return _intelliSenseProviders.ToList();
        }
    }

    public void RegisterProvider(IInlineCompletionProvider provider)
    {
        lock (_lock)
        {
            _inlineProviders.RemoveAll(p => p.Id == provider.Id);
            _inlineProviders.Add(provider);
        }
    }

    public void RegisterIntelliSenseProvider(IIntelliSenseProvider provider)
    {
        lock (_lock)
        {
            _intelliSenseProviders.RemoveAll(p => p.Id == provider.Id);
            _intelliSenseProviders.Add(provider);
        }
    }

    public void UnregisterProvider(string providerId)
    {
        lock (_lock)
        {
            _inlineProviders.RemoveAll(p => p.Id == providerId);
            _intelliSenseProviders.RemoveAll(p => p.Id == providerId);
        }
    }

    public async Task<string?> GetSuggestionAsync(InlineCompletionContext context, CancellationToken cancellationToken = default)
    {
        List<IInlineCompletionProvider> activeProviders;
        lock (_lock)
        {
            activeProviders = _inlineProviders.Where(p =>
                p.SupportedLanguages.Contains("*") ||
                p.SupportedLanguages.Any(l => l.Equals(context.LanguageId, StringComparison.OrdinalIgnoreCase))
            ).ToList();
        }

        foreach (var provider in activeProviders)
        {
            if (cancellationToken.IsCancellationRequested) return null;

            try
            {
                var suggestion = await provider.GetInlineSuggestionAsync(context, cancellationToken);
                if (!string.IsNullOrEmpty(suggestion))
                {
                    return suggestion;
                }
            }
            catch
            {
                // Extension failures must not crash the host editor
            }
        }

        return null;
    }

    public async Task<IReadOnlyList<CompletionItem>> GetCustomCompletionsAsync(string languageId, string wordPrefix, CancellationToken cancellationToken = default)
    {
        List<IIntelliSenseProvider> activeProviders;
        lock (_lock)
        {
            activeProviders = _intelliSenseProviders.Where(p =>
                p.SupportedLanguages.Contains("*") ||
                p.SupportedLanguages.Any(l => l.Equals(languageId, StringComparison.OrdinalIgnoreCase))
            ).ToList();
        }

        var results = new List<CompletionItem>();
        foreach (var provider in activeProviders)
        {
            if (cancellationToken.IsCancellationRequested) break;
            try
            {
                var items = await provider.GetCompletionsAsync(languageId, wordPrefix, cancellationToken);
                if (items != null) results.AddRange(items);
            }
            catch { }
        }

        return results;
    }
}
