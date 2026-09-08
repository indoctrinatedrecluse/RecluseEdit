namespace RecluseEdit.Core.Models;

/// <summary>
/// Defines a programming or markup language supported by RecluseEdit.
/// </summary>
public class LanguageDefinition
{
    /// <summary>
    /// Unique identifier for the language, e.g. "html", "javascript", "css", "csharp".
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// User-facing display name, e.g. "HTML", "JavaScript", "CSS".
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// File extensions associated with this language (including dot, e.g. ".html", ".htm").
    /// </summary>
    public required IReadOnlyList<string> Extensions { get; init; }

    /// <summary>
    /// Name of the AvalonEdit highlighting definition, if any.
    /// </summary>
    public string? HighlightingName { get; init; }

    public override string ToString() => DisplayName;
}

