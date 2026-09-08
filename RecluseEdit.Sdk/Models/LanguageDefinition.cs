namespace RecluseEdit.Sdk.Models;

/// <summary>
/// Defines a language supported by RecluseEdit.
/// </summary>
public class LanguageDefinition
{
    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public required IReadOnlyList<string> Extensions { get; init; }
    public string? HighlightingName { get; init; }

    public override string ToString() => DisplayName;
}
