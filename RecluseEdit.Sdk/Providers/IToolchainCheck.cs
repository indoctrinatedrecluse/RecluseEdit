using RecluseEdit.Sdk.Models;

namespace RecluseEdit.Sdk.Providers;

/// <summary>
/// Verifies prerequisites such as compilers, runtimes, or CLI utilities on the system PATH.
/// </summary>
public interface IToolchainCheck
{
    string ToolName { get; }
    string Command { get; }
    string? RequiredVersion { get; }
    string? InstallHelp { get; }

    Task<ToolchainReport> CheckAsync(CancellationToken cancellationToken = default);
}

