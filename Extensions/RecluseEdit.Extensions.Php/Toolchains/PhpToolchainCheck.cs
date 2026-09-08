using System.Diagnostics;
using System.Text.RegularExpressions;
using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Extensions.Php.Toolchains;

/// <summary>
/// Verifies the presence and version of the PHP CLI executable on the system PATH.
/// </summary>
public class PhpToolchainCheck : IToolchainCheck
{
    public string ToolName => "PHP CLI";
    public string Command => "php";
    public string? RequiredVersion => ">= 8.1.0";
    public string? InstallHelp => "Download PHP binaries from https://windows.php.net/download or install via: 'winget install PHP.PHP'";

    public async Task<ToolchainReport> CheckAsync(CancellationToken cancellationToken = default)
    {
        var (success, output, path) = await ExecuteCommandAsync("php", "-v", cancellationToken);

        if (!success || string.IsNullOrEmpty(output))
        {
            return new ToolchainReport
            {
                ToolName = ToolName,
                Command = Command,
                Status = ToolchainStatus.Warning,
                RequiredVersion = RequiredVersion,
                Description = "PHP CLI was not detected on system PATH. Install PHP to enable local script execution and server linting.",
                InstallHelp = InstallHelp
            };
        }

        // Example output: "PHP 8.3.4 (cli) (built: ...)"
        var versionMatch = Regex.Match(output, @"PHP\s+(\d+\.\d+\.\d+)");
        var version = versionMatch.Success ? versionMatch.Groups[1].Value : output.Split('\n')[0].Trim();

        return new ToolchainReport
        {
            ToolName = ToolName,
            Command = Command,
            Status = ToolchainStatus.Available,
            DetectedVersion = version,
            RequiredVersion = RequiredVersion,
            Description = $"PHP CLI detected ({version})",
            InstallHelp = InstallHelp,
            Path = path
        };
    }

    private static async Task<(bool success, string output, string? path)> ExecuteCommandAsync(string cmd, string args, CancellationToken ct)
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
            await p.WaitForExitAsync(ct);

            return (p.ExitCode == 0, stdout.Trim(), cmd);
        }
        catch
        {
            return (false, "", null);
        }
    }
}

/// <summary>
/// Verifies the presence and version of the Composer dependency manager on the system PATH.
/// </summary>
public class ComposerToolchainCheck : IToolchainCheck
{
    public string ToolName => "Composer";
    public string Command => "composer";
    public string? RequiredVersion => ">= 2.2.0";
    public string? InstallHelp => "Download Composer from https://getcomposer.org or install via: 'winget install Composer.Composer'";

    public async Task<ToolchainReport> CheckAsync(CancellationToken cancellationToken = default)
    {
        var (success, output, path) = await ExecuteCommandAsync("composer", "--version", cancellationToken);

        if (!success || string.IsNullOrEmpty(output))
        {
            return new ToolchainReport
            {
                ToolName = ToolName,
                Command = Command,
                Status = ToolchainStatus.Warning,
                RequiredVersion = RequiredVersion,
                Description = "Composer was not detected on system PATH. Required for managing PHP packages and autoloading.",
                InstallHelp = InstallHelp
            };
        }

        // Example output: "Composer version 2.7.2 2024-03-11 ..."
        var versionMatch = Regex.Match(output, @"Composer\s+version\s+(\d+\.\d+\.\d+)");
        var version = versionMatch.Success ? versionMatch.Groups[1].Value : output.Split('\n')[0].Trim();

        return new ToolchainReport
        {
            ToolName = ToolName,
            Command = Command,
            Status = ToolchainStatus.Available,
            DetectedVersion = version,
            RequiredVersion = RequiredVersion,
            Description = $"Composer detected ({version})",
            InstallHelp = InstallHelp,
            Path = path
        };
    }

    private static async Task<(bool success, string output, string? path)> ExecuteCommandAsync(string cmd, string args, CancellationToken ct)
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
            await p.WaitForExitAsync(ct);

            return (p.ExitCode == 0, stdout.Trim(), cmd);
        }
        catch
        {
            return (false, "", null);
        }
    }
}

