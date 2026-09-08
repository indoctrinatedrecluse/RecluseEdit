namespace RecluseEdit.Sdk.Models;

/// <summary>
/// Context passed to inline completion providers containing document state and caret position.
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
/// Item in an IntelliSense completion list.
/// </summary>
public class CompletionItem
{
    public required string Text { get; init; }
    public required string Description { get; init; }
    public string? InsertText { get; init; }
    public double Priority { get; init; }
    public string? Category { get; init; }
}
