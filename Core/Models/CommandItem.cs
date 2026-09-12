namespace RecluseEdit.Core.Models;

/// <summary>
/// Represents an executable action, feature, or file navigation target in the Universal Command Palette.
/// </summary>
public class CommandItem
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Category { get; init; }
    public string? InputGestureText { get; init; }
    public required Action Action { get; init; }
    public string? Icon { get; init; }
    public string? Description { get; init; }

    public override string ToString() => string.IsNullOrWhiteSpace(Category) ? Title : $"{Category}: {Title}";
}

