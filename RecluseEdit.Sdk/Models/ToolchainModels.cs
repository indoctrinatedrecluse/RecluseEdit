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
    public string? SdkId { get; set; }
}

/// <summary>
/// Information about an automatically detected software development kit (SDK) or compiler suite.
/// </summary>
public class DetectedSdk
{
    public required string SdkId { get; init; }
    public required string DisplayName { get; init; }
    public string? Version { get; set; }
    public required string RootDirectory { get; init; }
    public required string MainExecutablePath { get; init; }
    public string Category { get; init; } = "Compiler & Runtime";
    public string? EnvironmentVariable { get; init; }
    public IReadOnlyList<string> SecondaryBinaries { get; init; } = [];
}

/// <summary>
/// Standard identifiers for known development kits and compiler suites.
/// </summary>
public static class KnownSdk
{
    public const string DotNet = "dotnet";
    public const string NodeJs = "nodejs";
    public const string Rust = "rust";
    public const string Go = "go";
    public const string Python = "python";
    public const string Java = "java";
    public const string Flutter = "flutter";
    public const string Php = "php";
    public const string Ruby = "ruby";
    public const string Elixir = "elixir";
    public const string VisualStudio = "msvc";
    public const string Llvm = "llvm";
    public const string Git = "git";
}

