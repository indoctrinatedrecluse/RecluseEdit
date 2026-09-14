using System.Diagnostics;
using System.Text.RegularExpressions;
using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Extensions.React.Toolchains;

/// <summary>
/// Verifies the presence of the Node.js JavaScript runtime on the system PATH.
/// </summary>
public class NodeJsToolchainCheck : IToolchainCheck
{
    public string ToolName => "Node.js";
    public string Command => "node";
    public string? RequiredVersion => ">= 18.0.0";
    public string? InstallHelp => "Download Node.js from https://nodejs.org or install via winget: 'winget install OpenJS.NodeJS'";

    public async Task<ToolchainReport> CheckAsync(CancellationToken cancellationToken = default)
    {
        var (success, output, path) = await ExecuteCommandAsync("node", "--version", cancellationToken);

        if (!success || string.IsNullOrEmpty(output))
        {
            return new ToolchainReport
            {
                ToolName = ToolName,
                Command = Command,
                Status = ToolchainStatus.Missing,
                RequiredVersion = RequiredVersion,
                Description = "Node.js was not detected on system PATH. Required for running React build tools.",
                InstallHelp = InstallHelp
            };
        }

        var versionMatch = Regex.Match(output, @"v?(\d+\.\d+\.\d+)");
        var version = versionMatch.Success ? versionMatch.Value : output;

        return new ToolchainReport
        {
            ToolName = ToolName,
            Command = Command,
            Status = ToolchainStatus.Available,
            DetectedVersion = version,
            RequiredVersion = RequiredVersion,
            Description = $"Node.js runtime detected ({version})",
            InstallHelp = InstallHelp,
            Path = path
        };
    }

    private static async Task<(bool success, string output, string? path)> ExecuteCommandAsync(string cmd, string args, CancellationToken ct)
    {
        var res = await RecluseEdit.Sdk.Toolchains.ToolchainExecutor.ExecuteAsync(cmd, args, null, null, 3000, ct);
        return (res.Success, res.Output, res.ResolvedPath);
    }
}

/// <summary>
/// Verifies the presence of npm (Node Package Manager) on the system PATH.
/// </summary>
public class NpmToolchainCheck : IToolchainCheck
{
    public string ToolName => "npm";
    public string Command => "npm";
    public string? RequiredVersion => ">= 9.0.0";
    public string? InstallHelp => "npm is bundled with Node.js. Install Node.js from https://nodejs.org";

    public async Task<ToolchainReport> CheckAsync(CancellationToken cancellationToken = default)
    {
        var (success, output, path) = await ExecuteCommandAsync("npm", "--version", cancellationToken);

        if (!success || string.IsNullOrEmpty(output))
        {
            return new ToolchainReport
            {
                ToolName = ToolName,
                Command = Command,
                Status = ToolchainStatus.Missing,
                RequiredVersion = RequiredVersion,
                Description = "npm package manager not found on PATH.",
                InstallHelp = InstallHelp
            };
        }

        return new ToolchainReport
        {
            ToolName = ToolName,
            Command = Command,
            Status = ToolchainStatus.Available,
            DetectedVersion = output,
            RequiredVersion = RequiredVersion,
            Description = $"npm package manager detected ({output})",
            InstallHelp = InstallHelp,
            Path = path
        };
    }

    private static async Task<(bool success, string output, string? path)> ExecuteCommandAsync(string cmd, string args, CancellationToken ct)
    {
        var res = await RecluseEdit.Sdk.Toolchains.ToolchainExecutor.ExecuteAsync(cmd, args, null, null, 3000, ct);
        return (res.Success, res.Output, res.ResolvedPath);
    }
}

/// <summary>
/// Verifies the presence of the TypeScript compiler (tsc) on the system PATH.
/// </summary>
public class TypeScriptToolchainCheck : IToolchainCheck
{
    public string ToolName => "TypeScript (tsc)";
    public string Command => "tsc";
    public string? RequiredVersion => ">= 5.0.0";
    public string? InstallHelp => "Install globally via: 'npm install -g typescript' or within project: 'npm install --save-dev typescript'";

    public async Task<ToolchainReport> CheckAsync(CancellationToken cancellationToken = default)
    {
        var (success, output, path) = await ExecuteCommandAsync("tsc", "--version", cancellationToken);

        if (!success || string.IsNullOrEmpty(output))
        {
            return new ToolchainReport
            {
                ToolName = ToolName,
                Command = Command,
                Status = ToolchainStatus.Warning,
                RequiredVersion = RequiredVersion,
                Description = "TypeScript compiler (tsc) was not detected. Type checking will be limited to editor diagnostics.",
                InstallHelp = InstallHelp
            };
        }

        var versionMatch = Regex.Match(output, @"Version\s+([0-9\.]+)");
        var version = versionMatch.Success ? versionMatch.Value : output;

        return new ToolchainReport
        {
            ToolName = ToolName,
            Command = Command,
            Status = ToolchainStatus.Available,
            DetectedVersion = version,
            RequiredVersion = RequiredVersion,
            Description = $"TypeScript compiler detected ({version})",
            InstallHelp = InstallHelp,
            Path = path
        };
    }

    private static async Task<(bool success, string output, string? path)> ExecuteCommandAsync(string cmd, string args, CancellationToken ct)
    {
        var res = await RecluseEdit.Sdk.Toolchains.ToolchainExecutor.ExecuteAsync(cmd, args, null, null, 3000, ct);
        return (res.Success, res.Output, res.ResolvedPath);
    }
}
