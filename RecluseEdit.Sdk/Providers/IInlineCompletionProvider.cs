using RecluseEdit.Sdk.Models;

namespace RecluseEdit.Sdk.Providers;

/// <summary>
/// Provider interface for inline ghost-text suggestions ahead of the caret.
/// </summary>
public interface IInlineCompletionProvider
{
    string Id { get; }
    string Name { get; }
    IReadOnlyList<string> SupportedLanguages { get; }

    Task<string?> GetInlineSuggestionAsync(InlineCompletionContext context, CancellationToken cancellationToken = default);
}
