using RecluseEdit.Extensions;

namespace RecluseEdit.Core.Services;

/// <summary>
/// Aggregates and coordinates inline completion providers.
/// </summary>
public class AutocompleteManager
{
    private readonly List<IInlineCompletionProvider> _providers = [];
    private readonly object _lock = new();

    public IReadOnlyList<IInlineCompletionProvider> Providers
    {
        get
        {
            lock (_lock)
            {
                return _providers.ToList();
            }
        }
    }

    public void RegisterProvider(IInlineCompletionProvider provider)
    {
        lock (_lock)
        {
            _providers.RemoveAll(p => p.Id == provider.Id);
            _providers.Add(provider);
        }
    }

    public void UnregisterProvider(string providerId)
    {
        lock (_lock)
        {
            _providers.RemoveAll(p => p.Id == providerId);
        }
    }

    public async Task<string?> GetSuggestionAsync(InlineCompletionContext context, CancellationToken cancellationToken = default)
    {
        List<IInlineCompletionProvider> activeProviders;
        lock (_lock)
        {
            activeProviders = _providers.Where(p =>
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
                // Providers should not crash the editor on failure
            }
        }

        return null;
    }
}

