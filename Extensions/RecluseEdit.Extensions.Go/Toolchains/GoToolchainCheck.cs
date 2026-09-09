using System.Diagnostics;
using System.Text.RegularExpressions;
using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Extensions.Go.Toolchains;

/// <summary>
/// Verifies the presence, version, and architecture of the Go compiler and toolchain on the system PATH.
/// </summary>
public class GoCompilerToolchainCheck : IToolchainCheck
{
    public string ToolName => "Go Compiler";
    public string Command => "go";
    public string? RequiredVersion => ">= 1.20.0";
    public string? InstallHelp => "Download and install Go from https://go.dev/dl/ or via Windows Package Manager: 'winget install GoLang.Go'";

    public async Task<ToolchainReport> CheckAsync(CancellationToken cancellationToken = default)
    {
        var (success, output, path) = await ExecuteCommandAsync("go", "version", cancellationToken);

        if (!success || string.IsNullOrEmpty(output))
        {
            return new ToolchainReport
            {
                ToolName = ToolName,
                Command = Command,
                Status = ToolchainStatus.Missing,
                RequiredVersion = RequiredVersion,
                Description = "Go compiler was not detected on system PATH. Install Go to compile and run Go backend services.",
                InstallHelp = InstallHelp
            };
        }

        // Example output: "go version go1.23.1 windows/amd64" or "go version go1.27.0 windows/amd64"
        var match = Regex.Match(output, @"go\s+version\s+(go[0-9a-zA-Z\.\-]+)(\s+([a-zA-Z0-9_/]+))?");
        var version = match.Success ? match.Groups[1].Value : output.Split('\n')[0].Trim();
        var platform = match.Success && match.Groups[3].Success ? match.Groups[3].Value : "";

        return new ToolchainReport
        {
            ToolName = ToolName,
            Command = Command,
            Status = ToolchainStatus.Available,
            DetectedVersion = version,
            RequiredVersion = RequiredVersion,
            Description = !string.IsNullOrEmpty(platform)
                ? $"Go toolchain detected ({version} for {platform})"
                : $"Go toolchain detected ({version})",
            InstallHelp = InstallHelp,
            Path = path
        };
    }

    internal static async Task<(bool success, string output, string? path)> ExecuteCommandAsync(string cmd, string args, CancellationToken ct)
    {
        try
        {
            var isWindows = OperatingSystem.IsWindows();
            var fileName = isWindows ? "cmd.exe" : cmd;
            var arguments = isWindows ? $"/c {cmd} {args}" : args;

            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var p = Process.Start(psi);
            if (p == null) return (false, "", null);

            var stdout = await p.StandardOutput.ReadToEndAsync(ct);
            var stderr = await p.StandardError.ReadToEndAsync(ct);
            await p.WaitForExitAsync(ct);

            var outText = !string.IsNullOrWhiteSpace(stdout) ? stdout.Trim() : stderr.Trim();
            return (p.ExitCode == 0, outText, cmd);
        }
        catch
        {
            return (false, "", null);
        }
    }
}

/// <summary>
/// Verifies the presence and version of the popular golangci-lint static analysis linter.
/// </summary>
public class GolangciLintToolchainCheck : IToolchainCheck
{
    public string ToolName => "golangci-lint";
    public string Command => "golangci-lint";
    public string? RequiredVersion => ">= 1.50.0";
    public string? InstallHelp => "Install golangci-lint via Go: 'go install github.com/golangci/golangci-lint/cmd/golangci-lint@latest' or via brew/scoop.";

    public async Task<ToolchainReport> CheckAsync(CancellationToken cancellationToken = default)
    {
        var (success, output, path) = await GoCompilerToolchainCheck.ExecuteCommandAsync("golangci-lint", "--version", cancellationToken);

        if (!success || string.IsNullOrEmpty(output))
        {
            return new ToolchainReport
            {
                ToolName = ToolName,
                Command = Command,
                Status = ToolchainStatus.Warning,
                RequiredVersion = RequiredVersion,
                Description = "golangci-lint was not detected on system PATH. Recommended for comprehensive Go linting and static analysis.",
                InstallHelp = InstallHelp
            };
        }

        // Example output: "golangci-lint has version 1.60.3 built from (commit, date)"
        var versionMatch = Regex.Match(output, @"version\s+([0-9a-zA-Z\.\-]+)");
        var version = versionMatch.Success ? versionMatch.Groups[1].Value : output.Split('\n')[0].Trim();

        return new ToolchainReport
        {
            ToolName = ToolName,
            Command = Command,
            Status = ToolchainStatus.Available,
            DetectedVersion = version,
            RequiredVersion = RequiredVersion,
            Description = $"golangci-lint detected ({version})",
            InstallHelp = InstallHelp,
            Path = path
        };
    }
}
