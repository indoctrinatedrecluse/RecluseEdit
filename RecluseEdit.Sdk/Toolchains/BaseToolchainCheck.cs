using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Sdk.Toolchains;

/// <summary>
/// Abstract base class for toolchain and compiler checks providing automatic path resolution,
/// timeout protection, output parsing, and structured diagnostics reports.
/// </summary>
public abstract class BaseToolchainCheck : IToolchainCheck
{
    public abstract string ToolName { get; }
    public abstract string Command { get; }
    public virtual string? RequiredVersion => null;
    public virtual string? InstallHelp => null;

    /// <summary>
    /// Arguments used to query tool version (e.g. "--version", "version", "-v"). Defaults to "--version".
    /// </summary>
    public virtual string VersionArguments => "--version";

    /// <summary>
    /// Optional regex pattern used to extract semantic version numbers from stdout/stderr.
    /// </summary>
    public virtual string? VersionRegex => null;

    /// <summary>
    /// Optional associated SDK identifier (e.g. KnownSdk.DotNet, KnownSdk.Rust).
    /// </summary>
    public virtual string? SdkId => null;

    /// <summary>
    /// Process timeout in milliseconds before aborting check.
    /// </summary>
    public virtual int TimeoutMs => 5000;

    /// <summary>
    /// Diagnostic description generated when the tool is successfully detected.
    /// </summary>
    public virtual string DescriptionOnSuccess(string? version) =>
        $"{ToolName} detected ({version ?? "available"})";

    /// <summary>
    /// Diagnostic description generated when the tool is missing.
    /// </summary>
    public virtual string DescriptionOnMissing =>
        $"{ToolName} was not detected on system PATH or standard installation locations.";

    /// <summary>
    /// Status assigned when the tool is not found (Missing by default; can be overridden to Warning for optional tools).
    /// </summary>
    public virtual ToolchainStatus StatusOnMissing => ToolchainStatus.Missing;

    public virtual async Task<ToolchainReport> CheckAsync(CancellationToken cancellationToken = default)
    {
        var result = await ToolchainExecutor.ExecuteAsync(
            Command,
            VersionArguments,
            workspaceRoot: null,
            versionRegex: VersionRegex,
            timeoutMs: TimeoutMs,
            cancellationToken: cancellationToken);

        if (!result.Success || string.IsNullOrWhiteSpace(result.Output))
        {
            return new ToolchainReport
            {
                ToolName = ToolName,
                Command = Command,
                Status = StatusOnMissing,
                RequiredVersion = RequiredVersion,
                Description = DescriptionOnMissing,
                InstallHelp = InstallHelp,
                SdkId = SdkId,
                Path = result.ResolvedPath
            };
        }

        var version = result.DetectedVersion ?? result.Output.Split('\n')[0].Trim();

        return new ToolchainReport
        {
            ToolName = ToolName,
            Command = Command,
            Status = ToolchainStatus.Available,
            DetectedVersion = version,
            RequiredVersion = RequiredVersion,
            Description = DescriptionOnSuccess(version),
            InstallHelp = InstallHelp,
            Path = result.ResolvedPath,
            SdkId = SdkId
        };
    }
}

