namespace RecluseEdit.Extensions;

/// <summary>
/// Context passed to inline completion providers containing document state and caret information.
/// </summary>
public class InlineCompletionContext
{
    public required string TextBeforeCaret { get; init; }
    public required string CurrentLineText { get; init; }
    public required int CaretOffset { get; init; }
    public required int LineNumber { get; init; }
    public required int ColumnNumber { get; init; }
    public required string LanguageId { get; init; }
    public string? FilePath { get; init; }
    public required string FullText { get; init; }
}

/// <summary>
/// Provider interface for inline completion (ghost text suggestions).
/// </summary>
public interface IInlineCompletionProvider
{
    /// <summary>
    /// Unique provider identifier.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Display name of the provider.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// List of language IDs this provider supports (e.g. "html", "javascript", "css", or "*" for all).
    /// </summary>
    IReadOnlyList<string> SupportedLanguages { get; }

    /// <summary>
    /// Returns an inline ghost-text suggestion for the given context, or null if no suggestion is available.
    /// </summary>
    Task<string?> GetInlineSuggestionAsync(InlineCompletionContext context, CancellationToken cancellationToken = default);
}

