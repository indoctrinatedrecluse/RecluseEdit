namespace RecluseEdit.Core.Models;

/// <summary>
/// Represents a detected shell executable and configuration for terminal sessions.
/// </summary>
public class ShellInfo
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string ExecutablePath { get; set; } = string.Empty;
    public string Arguments { get; set; } = string.Empty;
    public string Icon { get; set; } = "💻";
    public bool IsDefault { get; set; }

    public override string ToString() => $"{Icon} {DisplayName}";
}
