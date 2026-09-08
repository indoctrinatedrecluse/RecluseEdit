using RecluseEdit.Sdk.Models;

namespace RecluseEdit.Sdk.Providers;

/// <summary>
/// Provider interface for popup IntelliSense dropdown completion items.
/// </summary>
public interface IIntelliSenseProvider
{
    string Id { get; }
    string Name { get; }
    IReadOnlyList<string> SupportedLanguages { get; }

    Task<IReadOnlyList<CompletionItem>> GetCompletionsAsync(string languageId, string wordPrefix, CancellationToken cancellationToken = default);
}

