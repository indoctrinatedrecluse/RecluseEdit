namespace RecluseEdit.Sdk.Models;

/// <summary>
/// Status state for a compiler, runtime, or CLI tool.
/// </summary>
public enum ToolchainStatus
{
    Unknown,
    Checking,
    Available,
    Missing,
    Warning
}

/// <summary>
/// Diagnostic report for an external tool requirement.
/// </summary>
public class ToolchainReport
{
    public required string ToolName { get; init; }
    public required string Command { get; init; }
    public ToolchainStatus Status { get; set; } = ToolchainStatus.Unknown;
    public string? DetectedVersion { get; set; }
    public string? RequiredVersion { get; init; }
    public string? Description { get; init; }
    public string? InstallHelp { get; init; }
    public string? Path { get; set; }
}

