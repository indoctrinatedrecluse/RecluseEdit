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
    public bool IsAvailable { get; set; } = true;
    public string[] ExpectedBinaryNames { get; set; } = [];
    public bool IsCustomConfigured { get; set; }
    public string Description { get; set; } = string.Empty;

    public override string ToString() => IsAvailable ? $"{Icon} {DisplayName}" : $"{Icon} {DisplayName} (not found)";
}

